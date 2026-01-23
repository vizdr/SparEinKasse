using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace WpfApplication1
{
    public class FilterViewModel : IViewFilters
    {
        private static FilterViewModel _instance;
        private static bool isDirty = false;

        public event RoutedEventHandler OnApplyFilter;
        public event RoutedEventHandler OnResetFilters;

        /// <summary>
        /// Gets the singleton instance. Prefer constructor injection over this method.
        /// </summary>
        public static FilterViewModel GetInstance()
        {
            return _instance ?? throw new InvalidOperationException("FilterViewModel not initialized. Use DI container.");
        }

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

            // Set static instance for legacy GetInstance() calls during transition
            _instance = this;
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

        public static void SetViewFilters(IViewFilters view)
        {
            _instance.BuchungstextValues = view.BuchungstextValues;
            _instance.UserAccounts = view.UserAccounts;
            _instance.ExpenciesLessThan = view.ExpenciesLessThan;
            _instance.ExpenciesMoreThan = view.ExpenciesMoreThan;
            _instance.IncomesLessThan = view.IncomesLessThan;
            _instance.IncomesMoreThan = view.IncomesMoreThan;
            isDirty = true;
        }

        public static void flopDirty()
        {
            if (isDirty)
                isDirty = false;
            else
                isDirty = true;
        }

        public static bool isFilterDirty()
        {
            return isDirty;
        }
        private void OnBuchungstextValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var receivedFrom = sender;
        }
    }
}
