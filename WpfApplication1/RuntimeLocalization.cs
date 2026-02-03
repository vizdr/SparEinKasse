using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using Local;

namespace WpfApplication1
{
    // RuntimeLocalization provides a singleton for notifying UI about culture changes.
    public sealed class RuntimeLocalization : INotifyPropertyChanged
    {
        public static RuntimeLocalization Instance { get; } = new RuntimeLocalization();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<CultureInfo> CultureChanged;

        public string this[string key] => Resource.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

        public void ChangeCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                throw new ArgumentNullException(nameof(cultureName));

            var culture = new CultureInfo(cultureName);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Also set Resource.Culture so strongly-typed resource access uses new culture
            Resource.Culture = culture;

            // Notify on UI dispatcher if available
            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)));
                    dispatcher.Invoke(() => CultureChanged?.Invoke(this, culture));
                }
                else
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                    CultureChanged?.Invoke(this, culture);
                }
            }
            catch
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                CultureChanged?.Invoke(this, culture);
            }
        }
    }
}
