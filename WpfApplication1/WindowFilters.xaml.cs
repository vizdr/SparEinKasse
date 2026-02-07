using System;
using System.Windows;
using System.Collections.ObjectModel;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for WindowFilters.xaml
    /// </summary>
    public partial class WindowFilters : Window , IViewFilters
    {
        private ObservableCollection<BoolTextCouple> buchungstextValues;
        private ObservableCollection<BoolTextCouple> userAccounts;
        public WindowFilters()
        {
            InitializeComponent();
            this.buttonCancel.Click += delegate { Hide(); };
        }

        public void RegisterEventHandlers()
        {
            this.buttonApply.Click += OnApplyFilter;
            this.buttonReset.Click += OnResetFilters;
        }

        #region IViewFilters Members

        public event RoutedEventHandler OnApplyFilter;
        public event RoutedEventHandler OnResetFilters;

        public ObservableCollection<BoolTextCouple> BuchungstextValues
        {
            get { return buchungstextValues; }
            set 
            {
                buchungstextValues = value;
                listViewBuchungsText.ItemsSource = buchungstextValues;
            }
        }

        public ObservableCollection<BoolTextCouple> UserAccounts
        {
            get { return userAccounts; }
            set
            {
                userAccounts = value;
                listViewAccounts.ItemsSource = userAccounts;
            }
        }

        public string ExpensesLessThan
        {
            get 
            {
                return comboBoxExpLess.Text;
            }

            set
            {
                comboBoxExpLess.Text = value ?? String.Empty;
            }
        }

        public string ExpensesMoreThan
        {
            get 
            { 
                return comboBoxExpMore.Text;
            }

            set
            {
                comboBoxExpMore.Text = value ?? String.Empty;
            }
        }

        public string IncomesLessThan
        {
            get 
            {
                return comboBoxIncomesLess.Text;
            }

            set
            {
                comboBoxIncomesLess.Text = value ?? String.Empty;
            }
        }

        public string IncomesMoreThan
        {
            get { return comboBoxIncomesMore.Text; }
            set { comboBoxIncomesMore.Text = value ?? String.Empty; }
        }

        public string ToFind
        {
            get { return textBoxFind.Text.Trim(); }
            set { textBoxFind.Text = value ?? String.Empty; }
        }

        #endregion
    }
}
