using System;
using System.Linq;
using Microsoft.Win32;

namespace WpfApplication1
{
    /// <summary>
    /// Manages application registration state and trial period.
    /// Reads/writes registration data from the Windows registry.
    /// </summary>
    public class RegistrationManager
    {
        private const string RegistrySubKey = "sskvz";
        private const string RegistryTrialKey = "isT";
        private const string RegistryExpirationKey = "ed";
        private const int TrialPeriodDays = 61;

        public bool IsNotRegistered { get; set; }
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Returns true if the user is registered or still within the trial period.
        /// </summary>
#if DEBUG
        public bool IsFeatureEnabled => true; // Always enabled in debug mode for testing
#else
        public bool IsFeatureEnabled => !IsNotRegistered || ExpirationDate > DateTime.Today;
#endif
        public RegistrationManager()
        {
            IsNotRegistered = true;
            ExpirationDate = DateTime.Today;
            LoadRegistrationState();
        }

        private void LoadRegistrationState()
        {
            using (RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true))
            {
                RegistryKey sskaKey = currentUserKey.OpenSubKey(RegistrySubKey, true);
                if (sskaKey != null)
                {
                    string[] kvalues = sskaKey.GetValueNames();
                    if (kvalues.Contains(RegistryTrialKey))
                    {
                        if (!bool.TryParse(sskaKey.GetValue(RegistryTrialKey).ToString(), out bool isNotReg))
                        {
                            sskaKey.SetValue(RegistryTrialKey, IsNotRegistered);
                        }
                        else
                        {
                            IsNotRegistered = isNotReg;
                        }
                    }
                    else
                    {
                        sskaKey.SetValue(RegistryTrialKey, true);
                    }
                    if (kvalues.Contains(RegistryExpirationKey))
                    {
                        if (!DateTime.TryParse(sskaKey.GetValue(RegistryExpirationKey).ToString(), out DateTime expDt))
                        {
                            ExpirationDate = DateTime.Today.Date.AddDays(TrialPeriodDays);
                            sskaKey.SetValue(RegistryExpirationKey, ExpirationDate.ToString("d"));
                        }
                        else
                        {
                            ExpirationDate = expDt;
                        }
                    }
                    else
                    {
                        sskaKey.SetValue(RegistryExpirationKey, DateTime.Now.Date.AddDays(TrialPeriodDays).ToString("d"));
                    }
                }
                else
                {
                    sskaKey = currentUserKey.CreateSubKey(RegistrySubKey);
                    sskaKey.SetValue(RegistryTrialKey, true);
                    sskaKey.SetValue(RegistryExpirationKey, DateTime.Now.Date.AddDays(TrialPeriodDays).ToString("d"));
                    ExpirationDate = DateTime.Today.Date.AddDays(TrialPeriodDays);
                }
                sskaKey.Close();
            }
        }

        /// <summary>
        /// Marks the application as activated in the registry.
        /// </summary>
        public void MarkAsRegistered()
        {
            IsNotRegistered = false;
            using (RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true))
            {
                RegistryKey sskaKey = currentUserKey.OpenSubKey(RegistrySubKey, true);
                if (sskaKey != null)
                {
                    sskaKey.SetValue(RegistryTrialKey, false);
                    sskaKey.SetValue(RegistryExpirationKey, DateTime.MaxValue.Date.ToString("d"));
                    sskaKey.Close();
                }
            }
        }
    }
}
