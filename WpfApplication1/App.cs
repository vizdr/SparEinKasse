using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Threading;
using WpfApplication1;

public class App : Application
{
    private readonly Window1 mainWindow;
    private readonly BusinessLogicSSKA businessLogic;
    private static App _instance;

    /// <summary>
    /// Constructor for DI container. Receives dependencies via injection.
    /// </summary>
    public App(Window1 mainWindow, BusinessLogicSSKA businessLogic)
    {
        this.mainWindow = mainWindow;
        this.businessLogic = businessLogic;
        _instance = this;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        CultureInfo CI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
        CI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
        Thread.CurrentThread.CurrentCulture = CI;
        mainWindow.Show();
        base.OnStartup(e);
    }

    public static void ChangeCulture(CultureInfo culture)
    {
        CultureInfo CI = (CultureInfo)culture.Clone();
        CI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
        Thread.CurrentThread.CurrentCulture = CI;
        Thread.CurrentThread.CurrentUICulture = CI;
        var oldWindow = Application.Current.MainWindow;

        // Create new Window1 using the BusinessLogicSSKA from DI
        Application.Current.MainWindow = new Window1(_instance.businessLogic);
        Application.Current.MainWindow.Show();
        if (!Application.Current.MainWindow.ShowActivated)
            Application.Current.MainWindow.Activate();
        Application.Current.MainWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
        oldWindow.Close();
    }
}
