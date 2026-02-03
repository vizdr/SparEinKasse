using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Windows.Controls.DataVisualization.Charting;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, IViewCharts, INotifyPropertyChanged
    {
        private readonly ChartsPresenter chP;
        private readonly BusinessLogicSSKA businessLogic;
        private readonly FilterViewModel filterViewModel;
        private WindowFilters windowFilter;

        private ObservableCollection<KeyValuePair<string, decimal>> incomes;
        private List<KeyValuePair<string, decimal>> remittees;

        public static bool isNotRegistred;
        public static DateTime expDate;
        private readonly TextBlock popupChDateExpText;
        private readonly TextBlock popupChRemiteExpText;
        private readonly TextBlock popupChCategExpText;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        static Window1()
        {
            isNotRegistred = true;
            expDate = DateTime.Today;
            HandleRegistration();
        }

        /// <summary>
        /// Constructor for DI container. Receives dependencies via injection.
        /// </summary>
        /// <summary>
        /// Constructor for DI container. Receives dependencies via injection.
        /// </summary>
        /// <param name="businessLogic">Business logic instance (injected)</param>
        /// <param name="filterViewModel">Filter view model (injected)</param>
        /// <param name="suppressActivationDialog">When true, do not show the activation dialog even if the app is not registered. Used when recreating the main window (e.g., on culture change).</param>
        public Window1(BusinessLogicSSKA businessLogic, FilterViewModel filterViewModel, bool suppressActivationDialog = false)
        {
            DiagnosticLog.Log("Window1", $"Constructor called (suppressActivationDialog={suppressActivationDialog})");
            DiagnosticLog.LogCultureState("Window1.ctor (start)");

            this.businessLogic = businessLogic ?? throw new ArgumentNullException(nameof(businessLogic));
            this.filterViewModel = filterViewModel ?? throw new ArgumentNullException(nameof(filterViewModel));
            try
            {
                DiagnosticLog.Log("Window1", "Calling InitializeComponent...");
                InitializeComponent();
                DiagnosticLog.Log("Window1", "InitializeComponent completed");
            }
            catch (XmlException exc)
            {
                DiagnosticLog.Log("Window1", $"EXCEPTION in InitializeComponent: {exc.Message}");
                MessageBox.Show(exc.InnerException.ToString(), "SSKA analyzer: Unable to initialize XAML components", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            chP = new ChartsPresenter(this, businessLogic, filterViewModel);
            chP.Initialize();
            this.Closing += delegate { chP.FinalizeChP(); };

#if DEBUG
            isNotRegistred = true;
#endif
            // Show activation dialog only on initial startup. When the main window is recreated
            // (for example during a language change), the caller can suppress the activation
            // dialog by passing suppressActivationDialog = true.
            if (isNotRegistred && !suppressActivationDialog)
            {
                WindowAc aw = new WindowAc();
                aw.Activate();
                aw.ShowDialog();
            }

            buttonUpdateSpan.Click += delegate { chP.Initialize(); };
            buttonShowFilters.Click += delegate { InitialaizeFiltersWindow(); };
            buttonUpdateDataBankXML.Click += delegate
            {
                if (!isNotRegistred || (expDate > DateTime.Today))
                {
                    chP.ReloadXml();
                    chP.Initialize();
                    InitializeComponent();
                }
            };
            buttonSettings.Click += delegate
            {
                // Remember current culture before opening settings
                string cultureBefore = Thread.CurrentThread.CurrentUICulture.Name;
                DiagnosticLog.Log("Window1", $"Opening Settings dialog, current culture: {cultureBefore}");

                // Show settings dialog (modal - blocks until closed)
                // Show settings dialog (modal - blocks until closed)
                var settingsDlg = new WindowFieldsDictionary();
                settingsDlg.Owner = this;
                settingsDlg.ShowDialog();

                // Ensure main window remains the application's MainWindow and is visible/activated
                try
                {
                    if (Application.Current != null)
                    {
                        Application.Current.MainWindow = this;
                    }
                    if (!this.IsVisible)
                    {
                        this.Show();
                    }
                    try { this.Activate(); } catch { }
                }
                catch (Exception ex)
                {
                    DiagnosticLog.Log("Window1", $"Failed to restore main window after settings dialog: {ex.Message}");
                }

                // After dialog closes, check if culture changed
                var appCultures = WpfApplication1.Properties.Settings.Default.AppCultures;
                string cultureAfter = appCultures != null && appCultures.Count > 0 ? appCultures[0] : cultureBefore;
                DiagnosticLog.Log("Window1", $"Settings dialog closed, saved culture: {cultureAfter}");

                if (!string.Equals(cultureBefore, cultureAfter, StringComparison.OrdinalIgnoreCase))
                {
                    DiagnosticLog.Log("Window1", $"Culture changed from {cultureBefore} to {cultureAfter}, prompting restart...");

                    // Get localized strings based on the NEW culture (the one user selected)
                    string title, message;
                    if (cultureAfter.StartsWith("de", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "Neustart erforderlich";
                        message = "Bitte starten Sie die Anwendung neu, damit die Sprachänderung wirksam wird.\n\nJetzt neu starten?";
                    }
                    else if (cultureAfter.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "Требуется перезапуск";
                        message = "Пожалуйста, перезапустите приложение для применения изменения языка.\n\nПерезапустить сейчас?";
                    }
                    else
                    {
                        title = "Restart Required";
                        message = "Please restart the application for the language change to take effect.\n\nRestart now?";
                    }

                    var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        DiagnosticLog.Log("Window1", "User chose to restart, restarting application...");
                        // Restart the application
                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        DiagnosticLog.Log("Window1", "User chose not to restart");
                    }
                }
                else
                {
                    DiagnosticLog.Log("Window1", "Culture unchanged, no action needed");
                }
            };

            lineSeriesDateExp.MouseUp += BarDataPoint_MouseUpDatExp;  // event handler popup for chart date-expence
            popupChDateExpText = new TextBlock
            {
                Background = Brushes.LightBlue,
                Padding = new Thickness(2.0d),
            };
            popupChRemiteExpText = new TextBlock
            {
                Background = Brushes.LightBlue,
                Padding = new Thickness(2.0d)
            };
            popupChCategExpText = new TextBlock
            {
                Background = Brushes.LightBlue,
                Padding = new Thickness(2.0d)
            };

            DiagnosticLog.Log("Window1", "Calling InitializeResources from constructor...");
            InitializeResources();

            // Subscribe to runtime localization changes so we can refresh UI texts in-place
            RuntimeLocalization.Instance.PropertyChanged += (s, ev) =>
            {
                DiagnosticLog.Log("Window1", "RuntimeLocalization.PropertyChanged fired - updating resources");
                // Ensure we update on UI thread
                Dispatcher.BeginInvoke(new Action(() => InitializeResources()));
            };

            // Additionally listen to CultureChanged to trigger a safe data refresh
            RuntimeLocalization.Instance.CultureChanged += (s, culture) =>
            {
                DiagnosticLog.Log("Window1", $"RuntimeLocalization.CultureChanged fired - culture: {culture?.Name}");
                // Make sure progress window can be shown
                if (businessLogic != null)
                    businessLogic.SuppressProgressWindow = false;

                // Trigger refresh on dispatcher to avoid blocking UI thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { businessLogic?.Request.ForceRefresh(); } catch { }
                }), DispatcherPriority.Background);
            };

            DiagnosticLog.Log("Window1", "Constructor completed successfully");
        }

        public void InitializeResources()
        {
            DiagnosticLog.Log("Window1", "InitializeResources called");
            DiagnosticLog.LogCultureState("Window1.InitializeResources");

            // Log some sample values to verify localization is working
            string incomesValue = RuntimeLocalization.Instance["Incomes"];
            string settingsValue = RuntimeLocalization.Instance["Settings"];
            DiagnosticLog.Log("Window1", $"RuntimeLocalization['Incomes'] = '{incomesValue}'");
            DiagnosticLog.Log("Window1", $"RuntimeLocalization['Settings'] = '{settingsValue}'");

            chartIncomes.Title = incomesValue;
            chartRemeteeExpence.Title = RuntimeLocalization.Instance["ExpencesOverRemittee"];
            chartDateExpence.Title = RuntimeLocalization.Instance["ExpencesOverDate"];
            chartDateBalance.Title = RuntimeLocalization.Instance["Balance"];
            expRemitties.Header = RuntimeLocalization.Instance["Exp"];
            expInc.Header = RuntimeLocalization.Instance["Inc"];
            groupBoxDateInterval.Header = RuntimeLocalization.Instance["TimeSpan"];
            labelFrom.Content = RuntimeLocalization.Instance["DatumFrom"];
            labelTo.Content = RuntimeLocalization.Instance["DatumTo"];
            buttonUpdateSpan.Content = RuntimeLocalization.Instance["UpdateDateSpan"];
            buttonUpdateDataBankXML.Content = RuntimeLocalization.Instance["UpdateDataStorage"];
            txtBoxInc.Text = RuntimeLocalization.Instance["Incomes"];
            buttonSettings.Content = settingsValue;
            labelAccounts.Content = RuntimeLocalization.Instance["Accounts"];
            buttonShowFilters.Content = RuntimeLocalization.Instance["Filter"];
            expRemGroups.Header = RuntimeLocalization.Instance["Exp2"];
            chartRemGroupExpence.Title = RuntimeLocalization.Instance["ExpencesOverRemitteeGroups"];
            chartCategoryExpence.Title = RuntimeLocalization.Instance["ExpencesOverCategory"];

            DiagnosticLog.Log("Window1", "InitializeResources completed");
        }

        private void InitialaizeFiltersWindow()
        {
            if(windowFilter == null)
            {
                windowFilter = new WindowFilters();
                windowFilter.Owner = GetWindow(this);
                windowFilter.Show();
                windowFilter.Activate();
                               
                chP.ViewFilters = windowFilter;
                windowFilter.OnApplyFilter += delegate { chP.ApplyFilters(); };
                windowFilter.OnResetFilters += delegate { chP.ResetFilters(); };
                windowFilter.RegisterEventHandlers();
            }
                          
            chP.InitializeFilters(windowFilter);
            windowFilter.Show();
        }

        #region IViewCharts Members

        public List<KeyValuePair<string, decimal>> Remitties
        {
            set
            {
                remittees = value;
                int qtyBars = remittees.Count();
                switch (qtyBars)
                {
                    case 1:
                        (chartRemeteeExpence.Series[0] as BarSeries).MaxHeight = 120;
                        break;
                    case 2:
                        (chartRemeteeExpence.Series[0] as BarSeries).MaxHeight = 450;
                        break;
                    default:
                        (chartRemeteeExpence.Series[0] as BarSeries).MaxHeight = double.PositiveInfinity;
                        break;
                }
                (chartRemeteeExpence.Series[0] as BarSeries).Refresh();
                (chartRemeteeExpence.Series[0] as BarSeries).DataContext = value;             
            }
        }
        public List<KeyValuePair<DateTime, decimal>> Expenses
        {
            set
            {
                (chartDateExpence.Series[0] as DataPointSeries).Refresh();
                (chartDateExpence.Series[0] as DataPointSeries).DataContext = value;
                
            }
        }
        public List<KeyValuePair<DateTime, decimal>> Balance
        {
            set
            {
                (chartDateBalance.Series[0] as DataPointSeries).Refresh();
                (chartDateBalance.Series[0] as DataPointSeries).DataContext = value;               
            }
        }
        public ObservableCollection<KeyValuePair<string, decimal>> Incomes
        {
            set
            {
                
                incomes = value;
                int qtyBars = incomes.Count;

                switch (qtyBars)
                {
                    case 1:
                        (chartIncomes.Series[0] as BarSeries).MaxHeight = 110;
                        break;
                    case 2:
                        (chartIncomes.Series[0] as BarSeries).MaxHeight = 400;
                        break;
                    default:
                        (chartIncomes.Series[0] as BarSeries).MaxHeight = double.PositiveInfinity;
                        break;
                }
                (chartIncomes.Series[0] as DataPointSeries).Refresh();
                (chartIncomes.Series[0] as DataPointSeries).DataContext = incomes;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(value)));
                
            }
        }
        public DateTime BeginDate
        {
            get => datePickerBeginDate?.SelectedDate ?? DateTime.Today.Date.AddDays(-30);
            set => datePickerBeginDate.SelectedDate = value;
        }
        public DateTime EndDate
        {
            get => datePickerEndDate?.SelectedDate ?? DateTime.Today.Date.Date;
            set => datePickerEndDate.SelectedDate = value;
        }
        public List<KeyValuePair<string, string>> ExpensesOverview
        {
            set => listboxExpencesOverview.DataContext = value;
        }
        public List<KeyValuePair<string, string>> IncomsOverview
        {
            set => listboxIncomssOverview.DataContext = value;
        }
        public decimal AxeRemittiesExpencesMaxValue
        {
            set
            {
                LinearAxis adjustedAxis1 = new LinearAxis();
                adjustedAxis1.Orientation = AxisOrientation.X;
                adjustedAxis1.ShowGridLines = true;
                adjustedAxis1.ExtendRangeToOrigin = true;
                adjustedAxis1.Maximum = (double)value;
                (chartRemeteeExpence.Series[0] as BarSeries).DependentRangeAxis = adjustedAxis1;
            }
        }
        public string Summary
        {
            set 
            {
                if(textBoxTotal.Dispatcher.CheckAccess())
                { 
                    textBoxTotal.Text = value;
                }
                else 
                {
                    textBoxTotal.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new UpdateUIDelegateTextBox(delegate { textBoxTotal.Text = value; }), textBoxTotal);
                }
            }
        }
        public List<string> Accounts
        {
            set => listBoxAccounts.DataContext = value;
        }
        public List<KeyValuePair<string, decimal>> RemittieeGroups
        {
            set
            {
                (chartRemGroupExpence.Series[0] as DataPointSeries).Refresh();
                (chartRemGroupExpence.Series[0] as DataPointSeries).DataContext = value;
            }
        }
        public List<KeyValuePair<string, decimal>> ExpensesCategory
        {
            set
            {
                (chartCategoryExpence.Series[0] as DataPointSeries).Refresh();
                (chartCategoryExpence.Series[0] as DataPointSeries).DataContext = value;               
            }
        }
        public decimal AxeExpencesCategoryMaxValue 
        {
            set
            {
                LinearAxis adjustedAxis = new LinearAxis();
                adjustedAxis.Orientation = AxisOrientation.X;
                adjustedAxis.ShowGridLines = true;
                adjustedAxis.ExtendRangeToOrigin = true;
                adjustedAxis.Maximum = (double)value;
                (chartCategoryExpence.Series[0] as BarSeries).DependentRangeAxis = adjustedAxis;
                (chartCategoryExpence.Series[0] as BarSeries).Refresh();
            }
        }

        #endregion

        // handlers of event setters for attached in xaml styles, resources for chart popups   
        private void BarDataPoint_MouseUpDatExp(object sender, MouseButtonEventArgs e)
        {
            // also accessible via object o = (chartDateExpence.Series[0] as DataPointSeries).SelectedItem;
            if ((sender as DataPointSeries).SelectedItem is KeyValuePair<DateTime, decimal> kv)
            {
                popupChDateExpText.Text = chP.GetExpensesAtDate(kv.Key.Date);
                popupChartDateExpenes.Child = popupChDateExpText;
                popupChartDateExpenes.IsOpen = true;
                popupChartDateExpenes.StaysOpen = false;
                popupChartDateExpenes.BringIntoView();
            }
        }
        private void BarDataPoint_MouseUpRemExp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as DataPointSeries).SelectedItem is KeyValuePair<string, decimal> kv)
            {
                popupChRemiteExpText.Text = chP.GetDates4Remitee(kv.Key);
                popupChartDateRemitte.Child = popupChRemiteExpText;
                popupChartDateRemitte.IsOpen = true;
                popupChartDateRemitte.StaysOpen = false;
                popupChartDateRemitte.ForceCursor = true;
                popupChartDateRemitte.BringIntoView();

            }
        }
        private void BarDataPoint_MouseUpCatExp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as DataPointSeries).SelectedItem is KeyValuePair<string, decimal> kv)
            {
                popupChCategExpText.Text = chP.GetDateBeneficiary(kv.Key);
                popupChartCategExp.Child = popupChCategExpText;
                popupChartCategExp.IsOpen = true;
                popupChartCategExp.StaysOpen = false;
                popupChartCategExp.BringIntoView();
            }
        }

        private static void HandleRegistration()
        {
            using (RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true))
            {
                RegistryKey sskaKey = currentUserKey.OpenSubKey("sskvz", true);
                if (sskaKey != null)
                {
                    string[] kvalues = sskaKey.GetValueNames();
                    if (kvalues.Contains("isT"))
                    {
                        if (!bool.TryParse(sskaKey.GetValue("isT").ToString(), out isNotRegistred))
                        {
                            // isNotRegistred = true;
                            sskaKey.SetValue("isT", isNotRegistred);
                        }
                        else { }
                    }
                    else
                    {
                        // isNotRegistred = true;
                        sskaKey.SetValue("isT", true);
                    }
                    if (kvalues.Contains("ed"))
                    {
                        if (!DateTime.TryParse(sskaKey.GetValue("ed").ToString(), out expDate))
                        {
                            expDate = DateTime.Today.Date.AddDays(61);
                            sskaKey.SetValue("ed", expDate.ToString("d"));
                        }
                        else { }
                    }
                    else
                    {
                        sskaKey.SetValue("ed", DateTime.Now.Date.AddDays(61).ToString("d"));
                    }
                }
                else
                {
                    sskaKey = currentUserKey.CreateSubKey("sskvz");
                    sskaKey.SetValue("isT", true);
                    sskaKey.SetValue("ed", DateTime.Now.Date.AddDays(61).ToString("d"));
                    // isNotRegistred = true;
                    expDate = DateTime.Today.Date.AddDays(61);
                }
                sskaKey.Close();
            }
        }
    }
}
