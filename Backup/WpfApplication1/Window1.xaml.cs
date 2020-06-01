using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApplication1;
using System.Xml.Linq;
using System.Xml;
using System.Windows.Markup;
using System.Windows.Controls.DataVisualization.Charting;
using Microsoft.Windows.Controls;
using System.Resources;
using WpfApplication1.Properties;
using Microsoft.Win32;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, IViewCharts
    {       
        private ChartsPresenter chP;
        private List<KeyValuePair<string, decimal>> incomes;
        private List<KeyValuePair<string, decimal>> remittees;
        private List<KeyValuePair<string, decimal>> remittieeGroups;
        public static bool isTr;
        public static DateTime expDate;
        
        static Window1()
        {
            using( RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true))
            {
                RegistryKey sskaKey = currentUserKey.OpenSubKey("sskvz", true);
                if (sskaKey != null)
                {
                    string[] kvalues = sskaKey.GetValueNames();
                    if (kvalues.Contains("isT"))
                        if (!bool.TryParse(sskaKey.GetValue("isT").ToString(), out isTr))
                        {
                            isTr = true;
                            sskaKey.SetValue("isT", isTr);
                        }
                        else { }
                    else
                    {
                        isTr = true;
                        sskaKey.SetValue("isT", true);
                    }
                    if (kvalues.Contains("ed"))
                        if (!DateTime.TryParse(sskaKey.GetValue("ed").ToString(), out expDate))
                        {
                            expDate = DateTime.Now.Date.AddDays(61);
                            sskaKey.SetValue("ed", expDate.ToString("d"));
                        }
                        else { }
                    else sskaKey.SetValue("ed", DateTime.Now.Date.AddDays(61).ToString("d"));                                 
                }
                else
                {
                    sskaKey = currentUserKey.CreateSubKey("sskvz");
                    sskaKey.SetValue("isT", true);
                    sskaKey.SetValue("ed", DateTime.Now.Date.AddDays(61).ToString("d"));
                    isTr = true;
                    expDate = DateTime.Now.Date.AddDays(61);
                }                
                sskaKey.Close();
            }
         } 

        public Window1()
        {            
            try
            {
                InitializeComponent();
            }
            catch ( XmlException exc )
            {
                MessageBox.Show(exc.InnerException.ToString(), "SSKA analyzer: Unable to initialize XAML components",   MessageBoxButton.OK, MessageBoxImage.Error); 
            }
                    
            this.chP = new ChartsPresenter(this);
            chP.Initialaze();

#if DEBUG
 isTr = true;
#endif

            if (isTr)
                {
                    WindowAc aw = new WindowAc();
                    aw.ShowDialog();
                }
            this.buttonUpdateSpan.Click += OnDateIntervalChanged; /*delegate { this.chP.Initialaze();  };*/
            this.buttonShowFilters.Click += delegate { InitialaizeFiltersWindow(new WindowFilters()); };
            this.buttonUpdateDataBankXML.Click += delegate 
            { 
                if (!isTr | expDate > DateTime.Now)
                    { this.chP.ReloadXml(); this.chP.Initialaze(); } 
            };          
            this.buttonSettings.Click += delegate { new WindowFieldsDictionary().ShowDialog();  };

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
            txtBoxExpences.Text = Local.Resource.Expences;
            buttonSettings.Content = Local.Resource.Settings;
            labelAccounts.Content = Local.Resource.Accounts;
            buttonShowFilters.Content = Local.Resource.Filter;
            expRemGroups.Header = Local.Resource.Exp2;
            chartRemGroupExpence.Title = Local.Resource.ExpencesOverRemitteeGroups;
        }
       
        private void InitialaizeFiltersWindow(WindowFilters window)
        {
            window.Owner = Window1.GetWindow(this);
            chP.ViewFilters = window;
            window.RegisterEventHandlers();
            chP.InitializeFilters(ChartsPresenter.FilterValues);
            window.Activate();
            window.Show();
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
                (chartRemeteeExpence.Series[0] as DataPointSeries).ItemsSource = remittees;
            }
        }

        public List<KeyValuePair<DateTime, decimal>> Expenses
        {
            set
            {
                (chartDateExpence.Series[0] as DataPointSeries).ItemsSource = value;
            }
        }

        public List<KeyValuePair<DateTime, decimal>> Balance
        {
           
            set
            {
                (chartDateBalance.Series[0] as DataPointSeries).ItemsSource = value;
            }
        }

        public List<KeyValuePair<string, decimal>> Incomes 
        {
            set 
            {    
                 incomes = value;                
                 int qtyBars = incomes.Count;
                 switch (qtyBars)
                 {
                     case 1: (chartIncomes.Series[0] as BarSeries).MaxHeight = 110;
                         break;
                     case 2: (chartIncomes.Series[0] as BarSeries).MaxHeight = 400;
                         break;
                     default: (chartIncomes.Series[0] as BarSeries).MaxHeight = double.PositiveInfinity;
                         break;                             
                 }
                 (chartIncomes.Series[0] as DataPointSeries).ItemsSource = incomes;                         
            } 
        }
       
        public DateTime BeginDate
        {
            get { return    datePickerBeginDate.SelectedDate ?? DateTime.Now.Date.AddDays(-30); }
            set {  datePickerBeginDate.SelectedDate = value; } 
        }

        public DateTime EndDate
        {
            get { return  datePickerEndDate.SelectedDate ?? DateTime.Now.Date.Date; }
            set {  datePickerEndDate.SelectedDate = value; }
        }
        
        public List<KeyValuePair<string, string>> ExpensesOverview
        {
            set { listboxExpencesOverview.DataContext = value; }
        }

        public List<KeyValuePair<string, string>> IncomsOverview
        {
            set { listboxIncomssOverview.DataContext = value; }
        }

        public Decimal AxeRemittiesExpencesMaxValue
        {
            set 
            {
                LinearAxis adjustedAxis = new LinearAxis();
                adjustedAxis.Orientation = AxisOrientation.X;
                adjustedAxis.ShowGridLines = true;
                adjustedAxis.ExtendRangeToOrigin = true;
                adjustedAxis.Maximum = (double)value;
                (chartRemeteeExpence.Series[0] as BarSeries).DependentRangeAxis = adjustedAxis; 
            }
        }

        public string Summary
        {
            set { textBoxTotal.Text = value; }
        }

        public List<string> Accounts
        {
            set { listBoxAccounts.DataContext = value; }                  
        }

        public event RoutedEventHandler OnDateIntervalChanged;
 
        public List<KeyValuePair<string, decimal>> RemittieeGroups
        {
            set 
            {
                remittieeGroups =  value;
                (chartRemGroupExpence.Series[0] as DataPointSeries).ItemsSource = remittieeGroups; 
            }
        }

        #endregion
    }
}
