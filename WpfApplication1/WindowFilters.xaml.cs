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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
        }

        public void RegisterEventHandlers()
        {
            this.buttonApply.Click += OnApplyFilter;
            this.buttonReset.Click += OnResetFilters;
            this.buttonCancel.Click += delegate { this.Close(); };
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

        
    }
}
