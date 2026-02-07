using System.Windows;
using System.Collections.ObjectModel;

namespace WpfApplication1
{
    public interface IViewFilters
    {
        ObservableCollection<BoolTextCouple> BuchungstextValues { set; get; }
        ObservableCollection<BoolTextCouple> UserAccounts { set; get; }
        string ExpensesLessThan { get; set; }
        string ExpensesMoreThan { get; set; }
        string IncomesLessThan { get; set; }
        string IncomesMoreThan { get; set; }
        string ToFind { get; set; }
        event RoutedEventHandler OnApplyFilter;
        event RoutedEventHandler OnResetFilters;
    }
}
