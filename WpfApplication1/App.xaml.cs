using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Threading;
using System.Windows.Media;

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

            // Calculation of Display Resolution
            //PresentationSource MainWindowPresentationSource = PresentationSource.FromVisual(oldWindow);
            //Matrix m = MainWindowPresentationSource.CompositionTarget.TransformToDevice;
            //// DpiWidthFactor = m.M11;
            //// DpiHeightFactor = m.M22;
            //double ScreenHeight = SystemParameters.PrimaryScreenHeight * m.M22;
            //double ScreenWidth = SystemParameters.PrimaryScreenWidth * m.M11;

            Application.Current.MainWindow = new Window1();
            Application.Current.MainWindow.Show(); 
            if(!Application.Current.MainWindow.ShowActivated)
                Application.Current.MainWindow.Activate();
            Application.Current.MainWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
            oldWindow.Close();

        }

    }
}
