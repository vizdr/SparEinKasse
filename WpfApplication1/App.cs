using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Threading;
using WpfApplication1;
using Local;

public class App : Application
{
    private const string LOG_SOURCE = "App";
    private readonly Window1 mainWindow;
    private readonly BusinessLogicSSKA businessLogic;
    private readonly FilterViewModel filterViewModel;
    private readonly RegistrationManager registrationManager;
    private static App _instance;

    /// <summary>
    /// Constructor for DI container. Receives dependencies via injection.
    /// </summary>
    public App(Window1 mainWindow, BusinessLogicSSKA businessLogic, FilterViewModel filterViewModel, RegistrationManager registrationManager)
    {
        this.mainWindow = mainWindow;
        this.businessLogic = businessLogic;
        this.filterViewModel = filterViewModel;
        this.registrationManager = registrationManager;
        _instance = this;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        DiagnosticLog.Log(LOG_SOURCE, "OnStartup called");
        DiagnosticLog.LogCultureState(LOG_SOURCE + ".OnStartup");

        // Culture is already set in Program.Main, just show the window
        // Ensure Application.Current.MainWindow references the created main window
        // Ensure shutdown occurs only when the main window is closed explicitly.
        // This prevents transient dialogs from causing the app to exit if they close
        // while the main window is still expected to remain open.
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        Application.Current.MainWindow = mainWindow;
        DiagnosticLog.Log(LOG_SOURCE, $"MainWindow set, showing window...");
        mainWindow.Show();
        DiagnosticLog.Log(LOG_SOURCE, "MainWindow.Show() completed");
        base.OnStartup(e);
    }

    public static void ChangeCulture(CultureInfo culture)
    {
        DiagnosticLog.Log(LOG_SOURCE, $"ChangeCulture called with culture: {culture?.Name ?? "null"}");
        DiagnosticLog.LogCultureState(LOG_SOURCE + ".ChangeCulture (before)");

        if (_instance == null)
        {
            DiagnosticLog.Log(LOG_SOURCE, "ERROR: _instance is null, aborting ChangeCulture");
            return;
        }
        if (Application.Current == null)
        {
            DiagnosticLog.Log(LOG_SOURCE, "ERROR: Application.Current is null, aborting ChangeCulture");
            return;
        }

        // Check if app is shutting down
        if (Application.Current.Dispatcher.HasShutdownStarted)
        {
            DiagnosticLog.Log(LOG_SOURCE, "ERROR: Dispatcher is shutting down, aborting ChangeCulture");
            return;
        }

        CultureInfo CI = (CultureInfo)culture.Clone();
        CI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
        Thread.CurrentThread.CurrentCulture = CI;
        Thread.CurrentThread.CurrentUICulture = CI;

        // Set the Resource culture for localized strings
        Resource.Culture = CI;
        DiagnosticLog.Log(LOG_SOURCE, $"Culture set to: {CI.Name}");
        DiagnosticLog.LogCultureState(LOG_SOURCE + ".ChangeCulture (after setting)");

        // Get the current main window
        var oldMainWindow = Application.Current.MainWindow as Window1;
        DiagnosticLog.Log(LOG_SOURCE, $"Old MainWindow: {(oldMainWindow != null ? "exists" : "null")}, IsVisible: {oldMainWindow?.IsVisible}");

        Window1 newWindow = null;
        try
        {
            // Suppress progress window during window recreation to avoid conflicts
            _instance.businessLogic.SuppressProgressWindow = true;
            DiagnosticLog.Log(LOG_SOURCE, "SuppressProgressWindow set to true");

            // Create new Window1 FIRST with the new culture (while old window still exists)
            // This ensures app never has zero windows, avoiding shutdown mode issues
            DiagnosticLog.Log(LOG_SOURCE, "Creating new Window1...");
            newWindow = new Window1(_instance.businessLogic, _instance.filterViewModel, _instance.registrationManager, suppressActivationDialog: true);
            DiagnosticLog.Log(LOG_SOURCE, "New Window1 created successfully");

            // Set and show the new window BEFORE closing the old one
            DiagnosticLog.Log(LOG_SOURCE, "Setting new window as MainWindow...");
            Application.Current.MainWindow = newWindow;
            DiagnosticLog.Log(LOG_SOURCE, "Calling newWindow.Show()...");
            newWindow.Show();
            DiagnosticLog.Log(LOG_SOURCE, $"newWindow.Show() completed - IsVisible: {newWindow.IsVisible}, WindowState: {newWindow.WindowState}, Size: {newWindow.ActualWidth}x{newWindow.ActualHeight}");
            newWindow.Activate();
            DiagnosticLog.Log(LOG_SOURCE, $"newWindow.Activate() completed - IsActive: {newWindow.IsActive}");

            // NOW close the old window (after new one is visible)
            if (oldMainWindow != null)
            {
                DiagnosticLog.Log(LOG_SOURCE, "Closing old MainWindow...");
                oldMainWindow.Close();
                DiagnosticLog.Log(LOG_SOURCE, "Old MainWindow closed");
            }

            // Verify new window is still visible after old one closed
            DiagnosticLog.Log(LOG_SOURCE, $"After old window closed - newWindow.IsVisible: {newWindow.IsVisible}, WindowState: {newWindow.WindowState}");

            // Re-enable progress window and force data refresh
            _instance.businessLogic.SuppressProgressWindow = false;
            DiagnosticLog.Log(LOG_SOURCE, "SuppressProgressWindow set to false, calling ForceRefresh()...");
            _instance.businessLogic.Request.ForceRefresh();
            DiagnosticLog.Log(LOG_SOURCE, "ForceRefresh() completed");
        }
        catch (Exception ex)
        {
            DiagnosticLog.Log(LOG_SOURCE, $"EXCEPTION in ChangeCulture: {ex.GetType().Name}: {ex.Message}");
            DiagnosticLog.Log(LOG_SOURCE, $"Stack trace: {ex.StackTrace}");

            // Always reset SuppressProgressWindow on error
            try { _instance.businessLogic.SuppressProgressWindow = false; } catch { }

            // If something went wrong but we have a new window, try to show it
            if (newWindow != null && !newWindow.IsVisible)
            {
                DiagnosticLog.Log(LOG_SOURCE, "Attempting recovery - showing new window...");
                try
                {
                    Application.Current.MainWindow = newWindow;
                    newWindow.Show();
                    DiagnosticLog.Log(LOG_SOURCE, "Recovery successful");
                }
                catch (Exception ex2)
                {
                    DiagnosticLog.Log(LOG_SOURCE, $"Recovery FAILED: {ex2.Message}");
                }
            }

            // Re-throw to show error to user
            throw;
        }

        DiagnosticLog.Log(LOG_SOURCE, "ChangeCulture completed");
    }
}
