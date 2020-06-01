using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void ChangeCulture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            var oldWindow = Application.Current.MainWindow;

            Application.Current.MainWindow = new Window1();
            if(!Application.Current.MainWindow.IsActive)
                Application.Current.MainWindow.Activate();
            Application.Current.MainWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
            Application.Current.MainWindow.Show();           
            oldWindow.Close();

        }

    }
}
