using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfApplication1.DTO
{
    public class ResponseModel : INotifyPropertyChanged
    {
        public ResponseModel(FilterViewModel filterViewModel)
        {
            if (filterViewModel == null)
                throw new ArgumentNullException(nameof(filterViewModel));

            BuchungstextOverDateRange = filterViewModel.BuchungstextValues;
            TransactionsAccountsObsCollBoolTextCouple = filterViewModel.UserAccounts;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Chart Data Properties

        private List<KeyValuePair<string, decimal>> _expensesOverDateRange = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> ExpensesOverDateRange
        {
            get => _expensesOverDateRange;
            set => SetProperty(ref _expensesOverDateRange, value);
        }

        private ObservableCollection<KeyValuePair<string, decimal>> _incomesOverDatesRange = new ObservableCollection<KeyValuePair<string, decimal>>();
        public ObservableCollection<KeyValuePair<string, decimal>> IncomesOverDatesRange
        {
            get => _incomesOverDatesRange;
            set => SetProperty(ref _incomesOverDatesRange, value);
        }

        private List<KeyValuePair<DateTime, decimal>> _balanceOverDateRange = new List<KeyValuePair<DateTime, decimal>>();
        public List<KeyValuePair<DateTime, decimal>> BalanceOverDateRange
        {
            get => _balanceOverDateRange;
            set => SetProperty(ref _balanceOverDateRange, value);
        }

        private List<KeyValuePair<string, decimal>> _expensesOverRemiteeInDateRange = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> ExpensesOverRemiteeInDateRange
        {
            get => _expensesOverRemiteeInDateRange;
            set => SetProperty(ref _expensesOverRemiteeInDateRange, value);
        }

        private List<KeyValuePair<string, decimal>> _expensesOverRemiteeGroupsInDateRange = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> ExpensesOverRemiteeGroupsInDateRange
        {
            get => _expensesOverRemiteeGroupsInDateRange;
            set => SetProperty(ref _expensesOverRemiteeGroupsInDateRange, value);
        }

        private List<KeyValuePair<string, string>> _expensesInfoOverDateRange = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> ExpensesInfoOverDateRange
        {
            get => _expensesInfoOverDateRange;
            set => SetProperty(ref _expensesInfoOverDateRange, value);
        }

        private List<KeyValuePair<string, string>> _incomesInfoOverDateRange = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> IncomesInfoOverDateRange
        {
            get => _incomesInfoOverDateRange;
            set => SetProperty(ref _incomesInfoOverDateRange, value);
        }

        private List<KeyValuePair<string, decimal>> _expensesOverCategory = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> ExpensesOverCategory
        {
            get => _expensesOverCategory;
            set => SetProperty(ref _expensesOverCategory, value);
        }

        private string _summary = string.Empty;
        public string Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        private List<string> _transactionsAccounts = new List<string>();
        public List<string> TransactionsAccounts
        {
            get => _transactionsAccounts;
            set => SetProperty(ref _transactionsAccounts, value);
        }

        #endregion

        #region Filter Data Properties

        private ObservableCollection<BoolTextCouple> _buchungstextOverDateRange;
        public ObservableCollection<BoolTextCouple> BuchungstextOverDateRange
        {
            get => _buchungstextOverDateRange;
            set => SetProperty(ref _buchungstextOverDateRange, value);
        }

        private ObservableCollection<BoolTextCouple> _transactionsAccountsObsCollBoolTextCouple;
        public ObservableCollection<BoolTextCouple> TransactionsAccountsObsCollBoolTextCouple
        {
            get => _transactionsAccountsObsCollBoolTextCouple;
            set => SetProperty(ref _transactionsAccountsObsCollBoolTextCouple, value);
        }

        #endregion

        #region View Data Properties (for drill-down/detail views)

        private List<KeyValuePair<string, decimal>> _expensesAtDate = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> ExpensesAtDate
        {
            get => _expensesAtDate;
            set => SetProperty(ref _expensesAtDate, value);
        }

        private List<KeyValuePair<string, decimal>> _dates4RemiteeOverDateRange = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> Dates4RemiteeOverDateRange
        {
            get => _dates4RemiteeOverDateRange;
            set => SetProperty(ref _dates4RemiteeOverDateRange, value);
        }

        private List<KeyValuePair<string, decimal>> _expenseBeneficiary4CategoryOverDateRange = new List<KeyValuePair<string, decimal>>();
        public List<KeyValuePair<string, decimal>> ExpenseBeneficiary4CategoryOverDateRange
        {
            get => _expenseBeneficiary4CategoryOverDateRange;
            set => SetProperty(ref _expenseBeneficiary4CategoryOverDateRange, value);
        }

        // Keep old name for backward compatibility
        [Obsolete("Use ExpenseBeneficiary4CategoryOverDateRange instead")]
        public List<KeyValuePair<string, decimal>> ExpenceBeneficiary4CategoryOverDateRange
        {
            get => _expenseBeneficiary4CategoryOverDateRange;
            set => ExpenseBeneficiary4CategoryOverDateRange = value;
        }

        #endregion

        #region State Properties

        private bool _isDataReady;
        /// <summary>
        /// Indicates whether initial data loading has completed.
        /// Set to true when BackgroundWorker finishes populating all chart data.
        /// </summary>
        public bool IsDataReady
        {
            get => _isDataReady;
            set => SetProperty(ref _isDataReady, value);
        }

        private bool _updateDataRequired;
        public bool UpdateDataRequired
        {
            get => _updateDataRequired;
            set => SetProperty(ref _updateDataRequired, value);
        }

        #endregion

        #region INotifyPropertyChanged

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
