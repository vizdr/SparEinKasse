using System.Collections.Generic;
using System.Windows;

namespace WpfApplication1
{
    interface IViewAccounts
    {
        List<string> UserAccounts { set; }
        string SelectedAccount { get; set; }
        event RoutedEventHandler OnAccountsAdd;
    }
}
