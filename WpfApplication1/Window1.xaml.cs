using System;
using System.Collections.Generic;
using System.Linq;
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
        public Window1(BusinessLogicSSKA businessLogic, FilterViewModel filterViewModel)
        {
            this.businessLogic = businessLogic ?? throw new ArgumentNullException(nameof(businessLogic));
            this.filterViewModel = filterViewModel ?? throw new ArgumentNullException(nameof(filterViewModel));

            try
            {
                InitializeComponent();
            }
            catch (XmlException exc)
            {
                MessageBox.Show(exc.InnerException.ToString(), "SSKA analyzer: Unable to initialize XAML components", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            chP = new ChartsPresenter(this, businessLogic, filterViewModel);
            chP.Initialize();
            this.Closing += delegate { chP.FinalizeChP(); };

#if DEBUG
            isNotRegistred = true;
#endif
            if (isNotRegistred)
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
            buttonSettings.Click += delegate { new WindowFieldsDictionary().ShowDialog(); };

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

            InitializeResources();
        }

        public void InitializeResources()
        {
            chartIncomes.Title = Local.Resource.Incomes;
            chartRemeteeExpence.Title = Local.Resource.ExpencesOverRemittee;
            chartDateExpence.Title = Local.Resource.ExpencesOverDate;
            chartDateBalance.Title = Local.Resource.Balance;           
            expRemitties.Header = Local.Resource.Exp;
            expInc.Header = Local.Resource.Inc;
            groupBoxDateInterval.Header = Local.Resource.TimeSpan;
            labelFrom.Content = Local.Resource.DatumFrom;
            labelTo.Content = Local.Resource.DatumTo;
            buttonUpdateSpan.Content = Local.Resource.UpdateDateSpan;
            buttonUpdateDataBankXML.Content = Local.Resource.UpdateDataStorage;
            txtBoxInc.Text = Local.Resource.Incomes;
            buttonSettings.Content = Local.Resource.Settings;
            labelAccounts.Content = Local.Resource.Accounts;
            buttonShowFilters.Content = Local.Resource.Filter;
            expRemGroups.Header = Local.Resource.Exp2;
            chartRemGroupExpence.Title = Local.Resource.ExpencesOverRemitteeGroups;
            chartCategoryExpence.Title = Local.Resource.ExpencesOverCategory;
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
