using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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

        public string ExpenciesLessThan
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

        public string ExpenciesMoreThan
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
                return comboBoxIncomsLess.Text;
            }

            set
            {
                comboBoxIncomsLess.Text = value ?? String.Empty;
            }
        }

        public string IncomesMoreThan
        {
            get { return comboBoxIncomsMore.Text; }
            set { comboBoxIncomsMore.Text = value ?? String.Empty; }
        }

        public string ToFind
        {
            get { return textBoxFind.Text.Trim(); }
            set { textBoxFind.Text = value ?? String.Empty; }
        }

        #endregion
        private void OnBuchungstextValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var receivedFrom = sender;
        }

    }
}
