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
}
