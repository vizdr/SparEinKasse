using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApplication1
{
    public class FilterParams : IViewFilters
    {
        private static readonly FilterParams instance = new FilterParams();

        public event RoutedEventHandler OnApplyFilter;
        public event RoutedEventHandler OnResetFilters;

        public static FilterParams GetInstance()
        {
            return instance;
        }

        // Observable collection to sync visual representation of ListView with data sources
        public ObservableCollection<BoolTextCouple> BuchungstextValues { set; get; }
        public ObservableCollection<BoolTextCouple> UserAccounts { set; get; }
        public string ExpenciesLessThan { get; set; } = string.Empty;
        public string ExpenciesMoreThan { get; set; } = string.Empty;
        public string IncomesLessThan { get;  set; } = string.Empty;
        public string IncomesMoreThan { get;  set; } = string.Empty;
        public string ToFind { get; set; } = string.Empty;

        private FilterParams()
        {
            BuchungstextValues = new ObservableCollection<BoolTextCouple>();
            UserAccounts = new ObservableCollection<BoolTextCouple>();
        }
        public FilterParams(IViewFilters filters)
            : this()
        { 
            ExpenciesLessThan = filters?.ExpenciesLessThan ?? String.Empty;
            ExpenciesMoreThan = filters?.ExpenciesMoreThan ?? String.Empty;
            IncomesLessThan = filters?.IncomesLessThan ?? String.Empty;
            IncomesMoreThan = filters?.IncomesMoreThan ?? String.Empty;
            ToFind = filters?.ToFind ?? String.Empty;
            BuchungstextValues = filters?.BuchungstextValues ?? BuchungstextValues;
            UserAccounts = filters?.UserAccounts ?? UserAccounts;
        }
    }
}
