using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApplication1
{
    public class FilterViewModel : IViewFilters
    {
        private bool isDirty = false;

#pragma warning disable CS0067 // Required by IViewFilters but never raised in this data-holder class
        public event RoutedEventHandler OnApplyFilter;
        public event RoutedEventHandler OnResetFilters;
#pragma warning restore CS0067

        // Observable collection to sync visual representation of ListView with data sources
        public ObservableCollection<BoolTextCouple> BuchungstextValues { set; get; }
        public ObservableCollection<BoolTextCouple> UserAccounts { set; get; }
        public string ExpensesLessThan { get; set; } = string.Empty;
        public string ExpensesMoreThan { get; set; } = string.Empty;
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
        }
        public void ResetFilterViewModel()           
        { 
            ExpensesLessThan =  String.Empty;
            ExpensesMoreThan = String.Empty;
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
            ExpensesLessThan = view.ExpensesLessThan;
            ExpensesMoreThan = view.ExpensesMoreThan;
            IncomesLessThan = view.IncomesLessThan;
            IncomesMoreThan = view.IncomesMoreThan;
            isDirty = true;
        }

        public void ToggleDirty()
        {
            isDirty = !isDirty;
        }

        public bool IsFilterDirty()
        {
            return isDirty;
        }
    }
}
