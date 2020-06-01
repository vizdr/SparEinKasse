using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WpfApplication1
{
    interface IViewAccounts
    {
        List<string> UserAccounts { set; }
        string SelectedAccount { get; set; }
        event RoutedEventHandler OnAccountsAdd;      
    }
}
