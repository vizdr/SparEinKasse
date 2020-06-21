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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for WindowAccount.xaml
    /// </summary>
    public partial class WindowAccount : Window, IViewAccounts
    {    
        private AccountsPresenter accPresenter;
        public WindowAccount()
        {
            InitializeComponent();
            accPresenter = new AccountsPresenter(this);
            accPresenter.Initialize();
           
            buttonAddAccount.Click += OnButtonAddAccountClick;
            listBoxAccounts.SelectionChanged += OnListBoxAccountsSelectionChanged;
        }

        #region IViewAccounts Members

        public List<string> UserAccounts
        {         
            set
            {
                listBoxAccounts.ItemsSource = value;
            }
        }

        public string SelectedAccount
        {
            get
            {
                return textBoxAccounts.Text;
            }
            set
            {
                textBoxAccounts.Text = value;
            }
        }

        public event RoutedEventHandler OnAccountsAdd;

        private void OnButtonAddAccountClick(object sender, RoutedEventArgs args)
        {
            if (IsSelectedAccountValidated(SelectedAccount))
            {
                OnAccountsAdd(sender, args);
                this.Close();
            }
            else textBoxAccounts.Background = new SolidColorBrush(Colors.Red);
        }

        private void OnListBoxAccountsSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            SelectedAccount = listBoxAccounts.SelectedValue.ToString(); 
        }

        private bool IsSelectedAccountValidated(string userInput)
        {
            return !String.IsNullOrEmpty(userInput);
        }
        
        #endregion
    }
}
