using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace WpfApplication1
{
    public class FilterViewModel : IViewFilters
    {
        private bool isDirty = false;

        public event RoutedEventHandler OnApplyFilter;
        public event RoutedEventHandler OnResetFilters;

        // Observable collection to sync visual representation of ListView with data sources
        public ObservableCollection<BoolTextCouple> BuchungstextValues { set; get; }
        public ObservableCollection<BoolTextCouple> UserAccounts { set; get; }
        public string ExpenciesLessThan { get; set; } = string.Empty;
        public string ExpenciesMoreThan { get; set; } = string.Empty;
        public string IncomesLessThan { get;  set; } = string.Empty;
        public string IncomesMoreThan { get;  set; } = string.Empty;
        public string ToFind { get; set; } = string.Empty;

        /// <summary>
        /// Constructor for DI container.
        /// </summary>
        public FilterViewModel()
        {
            BuchungstextValues = new ObservableCollection<BoolTextCouple>();
            UserAccounts = new ObservableCollection<BoolTextCouple>();
            BuchungstextValues.CollectionChanged += OnBuchungstextValuesChanged;
        }
        public void ResetFilterViewModel()           
        { 
            ExpenciesLessThan =  String.Empty;
            ExpenciesMoreThan = String.Empty;
            IncomesLessThan =  String.Empty;
            IncomesMoreThan =  String.Empty;
            ToFind =  String.Empty;
            BuchungstextValues = new ObservableCollection<BoolTextCouple>();
            UserAccounts = new ObservableCollection<BoolTextCouple>();
            isDirty = true;
        }
        public bool IsFilterPrepared() 
        {
            return BuchungstextValues.Count > 0 || UserAccounts.Count > 0;
        }

        public void SetViewFilters(IViewFilters view)
        {
            BuchungstextValues = view.BuchungstextValues;
            UserAccounts = view.UserAccounts;
            ExpenciesLessThan = view.ExpenciesLessThan;
            ExpenciesMoreThan = view.ExpenciesMoreThan;
            IncomesLessThan = view.IncomesLessThan;
            IncomesMoreThan = view.IncomesMoreThan;
            isDirty = true;
        }

        public void FlopDirty()
        {
            isDirty = !isDirty;
        }

        public bool IsFilterDirty()
        {
            return isDirty;
        }
        private void OnBuchungstextValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var receivedFrom = sender;
        }
    }
}
