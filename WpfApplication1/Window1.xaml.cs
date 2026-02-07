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
        private readonly RegistrationManager _registration;
        private WindowFilters windowFilter;

        private ObservableCollection<KeyValuePair<string, decimal>> incomes;
        private List<KeyValuePair<string, decimal>> remittees;

        private readonly TextBlock popupChDateExpText;
        private readonly TextBlock popupChRemiteExpText;
        private readonly TextBlock popupChCategExpText;

        // Stored for unsubscription on window close to prevent singleton → window leaks
        private readonly PropertyChangedEventHandler _onLocalizationPropertyChanged;
        private readonly EventHandler<System.Globalization.CultureInfo> _onLocalizationCultureChanged;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        /// <summary>
        /// Constructor for DI container. Receives dependencies via injection.
        /// </summary>
        /// <param name="businessLogic">Business logic instance (injected)</param>
        /// <param name="filterViewModel">Filter view model (injected)</param>
        /// <param name="registration">Registration manager (injected)</param>
        /// <param name="suppressActivationDialog">When true, do not show the activation dialog even if the app is not registered. Used when recreating the main window (e.g., on culture change).</param>
        public Window1(BusinessLogicSSKA businessLogic, FilterViewModel filterViewModel, RegistrationManager registration, bool suppressActivationDialog = false)
        {
            DiagnosticLog.Log("Window1", $"Constructor called (suppressActivationDialog={suppressActivationDialog})");
            DiagnosticLog.LogCultureState("Window1.ctor (start)");

            this.businessLogic = businessLogic ?? throw new ArgumentNullException(nameof(businessLogic));
            this.filterViewModel = filterViewModel ?? throw new ArgumentNullException(nameof(filterViewModel));
            _registration = registration ?? throw new ArgumentNullException(nameof(registration));
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
            this.Closing += OnWindowClosing;

#if DEBUG
            _registration.IsNotRegistered = true;
#endif
            // Show activation dialog only on initial startup. When the main window is recreated
            // (for example during a language change), the caller can suppress the activation
            // dialog by passing suppressActivationDialog = true.
            if (_registration.IsNotRegistered && !suppressActivationDialog)
            {
                WindowAc aw = new WindowAc(_registration);
                aw.Activate();
                aw.ShowDialog();
            }

            buttonUpdateSpan.Click += delegate { chP.Initialize(); };
            buttonShowFilters.Click += delegate { InitializeFiltersWindow(); };
            buttonUpdateDataBankXML.Click += delegate
            {
                if (_registration.IsFeatureEnabled)
                {
                    chP.ReloadXml();
                    chP.Initialize();
                    InitializeComponent();
                }
            };
            buttonSettings.Click += delegate { OnSettingsClicked(); };

            // BarDataPoint_MouseUpDatExp is already wired via XAML EventSetter
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

            // Subscribe to runtime localization changes (store delegates for unsubscription on close)
            _onLocalizationPropertyChanged = (s, ev) =>
            {
                Dispatcher.BeginInvoke(new Action(() => InitializeResources()));
            };
            RuntimeLocalization.Instance.PropertyChanged += _onLocalizationPropertyChanged;

            _onLocalizationCultureChanged = (s, culture) =>
            {
                DiagnosticLog.Log("Window1", $"RuntimeLocalization.CultureChanged fired - culture: {culture?.Name}");
                if (businessLogic != null)
                    businessLogic.SuppressProgressWindow = false;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { businessLogic?.Request.ForceRefresh(); } catch { }
                }), DispatcherPriority.Background);
            };
            RuntimeLocalization.Instance.CultureChanged += _onLocalizationCultureChanged;

            DiagnosticLog.Log("Window1", "Constructor completed successfully");
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            chP.FinalizeChP();

            // Unsubscribe from singleton events to allow this window to be garbage collected
            RuntimeLocalization.Instance.PropertyChanged -= _onLocalizationPropertyChanged;
            RuntimeLocalization.Instance.CultureChanged -= _onLocalizationCultureChanged;

            // Clean up filter window
            if (windowFilter != null)
            {
                windowFilter.Close();
                windowFilter = null;
            }
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

        private void OnSettingsClicked()
        {
            string cultureBefore = Thread.CurrentThread.CurrentUICulture.Name;
            DiagnosticLog.Log("Window1", $"Opening Settings dialog, current culture: {cultureBefore}");

            var settingsDlg = new WindowFieldsDictionary();
            settingsDlg.Owner = this;
            settingsDlg.ShowDialog();

            // Ensure main window remains visible after modal dialog closes
            try
            {
                if (Application.Current != null)
                    Application.Current.MainWindow = this;
                if (!this.IsVisible)
                    this.Show();
                try { this.Activate(); } catch { }
            }
            catch (Exception ex)
            {
                DiagnosticLog.Log("Window1", $"Failed to restore main window after settings dialog: {ex.Message}");
            }

            // Check if culture changed and prompt restart
            var appCultures = WpfApplication1.Properties.Settings.Default.AppCultures;
            string cultureAfter = appCultures != null && appCultures.Count > 0 ? appCultures[0] : cultureBefore;
            DiagnosticLog.Log("Window1", $"Settings dialog closed, saved culture: {cultureAfter}");

            if (!string.Equals(cultureBefore, cultureAfter, StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticLog.Log("Window1", $"Culture changed from {cultureBefore} to {cultureAfter}, prompting restart...");
                PromptCultureRestart(cultureAfter);
            }
        }

        private static void PromptCultureRestart(string culture)
        {
            string title, message;
            if (culture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            {
                title = "Neustart erforderlich";
                message = "Bitte starten Sie die Anwendung neu, damit die Sprachänderung wirksam wird.\n\nJetzt neu starten?";
            }
            else if (culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
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
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Application.Current.Shutdown();
            }
            else
            {
                DiagnosticLog.Log("Window1", "User chose not to restart");
            }
        }

        private void InitializeFiltersWindow()
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
            set => listboxExpensesOverview.DataContext = value;
        }
        public List<KeyValuePair<string, string>> IncomesOverview
        {
            set => listboxIncomesOverview.DataContext = value;
        }
        public decimal AxeRemittiesExpensesMaxValue
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
        public decimal AxeExpensesCategoryMaxValue
        {
            set
            {
                LinearAxis adjustedAxis = new LinearAxis();
                adjustedAxis.Orientation = AxisOrientation.X;
                adjustedAxis.ShowGridLines = true;
                adjustedAxis.ExtendRangeToOrigin = true;
                adjustedAxis.Maximum = (double)value;
                (chartCategoryExpence.Series[0] as BarSeries).DependentRangeAxis = adjustedAxis;
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

    }
}
