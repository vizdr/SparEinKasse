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

    }
}
