using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using WpfApplication1;
using WpfApplication1.BusinessLogic;
using WpfApplication1.DAL;
using WpfApplication1.DTO;
using WpfApplication1.Properties;
using Local;

namespace WpfApplication1
{
    class Program
    {
        [STAThread]
        public static void Main()
        {
            // Start diagnostic logging session
            DiagnosticLog.LogSessionStart();
            DiagnosticLog.Log("Program.Main", "Application starting...");
            DiagnosticLog.LogCultureState("Program.Main (initial)");

            // Check if this is first run or app was reinstalled/upgraded
            bool shouldDetectSystemCulture = ShouldResetToSystemCulture();
            DiagnosticLog.Log("Program.Main", $"ShouldResetToSystemCulture returned: {shouldDetectSystemCulture}");

            // Set culture BEFORE creating any services that parse dates
            CultureInfo CI;
            string systemCulture = Thread.CurrentThread.CurrentUICulture.Name;
            var supportedCultures = Settings.Default.AppCultures;

            // Log settings state
            DiagnosticLog.Log("Program.Main", $"System UI Culture (from Windows): {systemCulture}");
            DiagnosticLog.Log("Program.Main", $"Settings.Default.IsFirstRun: {Settings.Default.IsFirstRun}");
            if (supportedCultures != null)
            {
                DiagnosticLog.Log("Program.Main", $"Settings.Default.AppCultures count: {supportedCultures.Count}");
                for (int i = 0; i < supportedCultures.Count; i++)
                {
                    DiagnosticLog.Log("Program.Main", $"  AppCultures[{i}]: {supportedCultures[i]}");
                }
            }
            else
            {
                DiagnosticLog.Log("Program.Main", "Settings.Default.AppCultures is NULL!");
            }

            if (shouldDetectSystemCulture)
            {
                DiagnosticLog.Log("Program.Main", "First run/reinstall detected - attempting to match system culture");
                // First run or reinstall: use Windows system culture if supported
                string matchedCulture = FindMatchingCulture(systemCulture, supportedCultures);
                DiagnosticLog.Log("Program.Main", $"FindMatchingCulture result: {matchedCulture ?? "null (no match)"}");

                if (matchedCulture != null)
                {
                    CI = CultureInfo.CreateSpecificCulture(matchedCulture);
                    DiagnosticLog.Log("Program.Main", $"Using matched culture: {CI.Name}");

                    // Rebuild the collection with matched culture first to ensure save works
                    var newCultures = new System.Collections.Specialized.StringCollection();
                    newCultures.Add(matchedCulture);
                    foreach (string culture in supportedCultures)
                    {
                        if (!string.Equals(culture, matchedCulture, StringComparison.OrdinalIgnoreCase))
                            newCultures.Add(culture);
                    }
                    Settings.Default.AppCultures = newCultures;
                    DiagnosticLog.Log("Program.Main", $"Reordered AppCultures - new first item: {newCultures[0]}");
                }
                else if (supportedCultures != null && supportedCultures.Count > 0)
                {
                    CI = CultureInfo.CreateSpecificCulture(supportedCultures[0]);
                    DiagnosticLog.Log("Program.Main", $"No match found, using first supported culture: {CI.Name}");
                }
                else
                {
                    CI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
                    DiagnosticLog.Log("Program.Main", $"No supported cultures, using thread culture: {CI.Name}");
                }

                // Mark first run as complete and save current version
                Settings.Default.IsFirstRun = false;
                SaveCurrentVersion();
                DiagnosticLog.Log("Program.Main", "Saving settings...");
                Settings.Default.Save();
                DiagnosticLog.Log("Program.Main", "Settings saved. IsFirstRun now: " + Settings.Default.IsFirstRun);
            }
            else if (supportedCultures != null && supportedCultures.Count > 0)
            {
                // Subsequent runs: use saved culture preference
                CI = CultureInfo.CreateSpecificCulture(supportedCultures[0]);
                DiagnosticLog.Log("Program.Main", $"Subsequent run - using saved culture preference: {CI.Name}");
            }
            else
            {
                CI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
                DiagnosticLog.Log("Program.Main", $"Fallback - using thread culture clone: {CI.Name}");
            }

            CI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            Thread.CurrentThread.CurrentCulture = CI;
            Thread.CurrentThread.CurrentUICulture = CI;
            Resource.Culture = CI;
            DiagnosticLog.Log("Program.Main", $"Final culture applied: {CI.Name}");
            DiagnosticLog.LogCultureState("Program.Main (after setting)");
            // Create application host
            var host = Host.CreateDefaultBuilder()
                // Register services with DI container
                .ConfigureServices(services =>
                {
                    // DTO singletons (order matters due to dependencies)
                    services.AddSingleton<FilterViewModel>();
                    services.AddSingleton<DataRequest>();
                    services.AddSingleton<ResponseModel>();

                    // DAL services - CsvToXmlSSKA implements IDataSourceProvider
                    services.AddSingleton<CsvToXmlSSKA>();
                    services.AddSingleton<IDataSourceProvider>(sp => sp.GetRequiredService<CsvToXmlSSKA>());
                    services.AddSingleton<AccountsLogic>(sp =>
                        new AccountsLogic(sp.GetRequiredService<IDataSourceProvider>()));

                    // Business Logic
                    services.AddSingleton<IBusinessLogic, BusinessLogicSSKA>();
                    services.AddSingleton<BusinessLogicSSKA>();

                    // Presenters
                    services.AddTransient<ChartsPresenter>();

                    // UI
                    services.AddSingleton<Window1>();
                    services.AddSingleton<App>();
                })
                .Build();

            // Wire up circular dependency (CsvToXmlSSKA <-> AccountsLogic)
            var csvToXml = host.Services.GetRequiredService<CsvToXmlSSKA>();
            csvToXml.AccountsLogic = host.Services.GetRequiredService<AccountsLogic>();

            // Get the App service and run
            var app = host.Services.GetService<App>();
            app?.Run();
        }

        /// <summary>
        /// Finds a matching culture from the supported cultures list.
        /// First tries exact match, then prefix match (e.g., "de" matches "de-DE").
        /// </summary>
        private static string FindMatchingCulture(string systemCulture, System.Collections.Specialized.StringCollection supportedCultures)
        {
            if (supportedCultures == null || string.IsNullOrEmpty(systemCulture))
                return null;

            // Try exact match first (case-insensitive)
            foreach (string culture in supportedCultures)
            {
                if (string.Equals(culture, systemCulture, StringComparison.OrdinalIgnoreCase))
                    return culture;
            }

            // Try prefix match: "de" should match "de-DE", "de-AT", etc.
            string systemPrefix = systemCulture.Split('-')[0].ToLowerInvariant();
            foreach (string culture in supportedCultures)
            {
                string culturePrefix = culture.Split('-')[0].ToLowerInvariant();
                if (culturePrefix == systemPrefix)
                    return culture;
            }

            return null;
        }

        /// <summary>
        /// Determines if the app should reset to system culture.
        /// Returns true on first run, reinstall, upgrade, or when installer flag is set.
        /// </summary>
        private static bool ShouldResetToSystemCulture()
        {
            DiagnosticLog.Log("ShouldResetToSystemCulture", "Checking conditions...");

            // Check 1: IsFirstRun setting (true by default for fresh user.config)
            bool isFirstRun = Settings.Default.IsFirstRun;
            DiagnosticLog.Log("ShouldResetToSystemCulture", $"Check 1 - IsFirstRun: {isFirstRun}");
            if (isFirstRun)
            {
                DiagnosticLog.Log("ShouldResetToSystemCulture", "Returning TRUE (IsFirstRun)");
                return true;
            }

            // Check 2: Version changed (indicates reinstall/upgrade)
            string currentVersion = GetCurrentVersion();
            string savedVersion = GetSavedVersion();
            DiagnosticLog.Log("ShouldResetToSystemCulture", $"Check 2 - CurrentVersion: {currentVersion}, SavedVersion: {savedVersion ?? "null"}");
            if (string.IsNullOrEmpty(savedVersion) || savedVersion != currentVersion)
            {
                DiagnosticLog.Log("ShouldResetToSystemCulture", "Returning TRUE (version mismatch)");
                return true;
            }

            // Check 3: Installer registry flag (backup mechanism)
            bool installerFlag = CheckInstallerFlag();
            DiagnosticLog.Log("ShouldResetToSystemCulture", $"Check 3 - InstallerFlag: {installerFlag}");
            if (installerFlag)
            {
                DiagnosticLog.Log("ShouldResetToSystemCulture", "Returning TRUE (installer flag)");
                return true;
            }

            DiagnosticLog.Log("ShouldResetToSystemCulture", "Returning FALSE (no conditions met)");
            return false;
        }

        /// <summary>
        /// Gets the current application version.
        /// </summary>
        private static string GetCurrentVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        /// <summary>
        /// Gets the saved version from registry.
        /// </summary>
        private static string GetSavedVersion()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\VZdravkov\SSKAanalyzer"))
                {
                    return key?.GetValue("AppVersion") as string;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Saves the current version to registry.
        /// </summary>
        private static void SaveCurrentVersion()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\VZdravkov\SSKAanalyzer"))
                {
                    key?.SetValue("AppVersion", GetCurrentVersion(), RegistryValueKind.String);
                }
            }
            catch
            {
                // Ignore registry errors
            }
        }

        /// <summary>
        /// Checks if the installer has set the ResetSettings flag and clears it.
        /// </summary>
        private static bool CheckInstallerFlag()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\VZdravkov\SSKAanalyzer", writable: true))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("ResetSettings");
                        if (value != null && Convert.ToInt32(value) == 1)
                        {
                            // Clear the flag
                            key.SetValue("ResetSettings", 0, RegistryValueKind.DWord);
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry access errors
            }
            return false;
        }
    }
}
