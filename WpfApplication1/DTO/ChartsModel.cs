using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfApplication1.DTO
{
    public class ResponseModel : INotifyPropertyChanged
    {
        private static ResponseModel _instance;

        /// <summary>
        /// Constructor for DI container. Receives FilterViewModel via injection.
        /// </summary>
        public ResponseModel(FilterViewModel filterViewModel)
        {
            if (filterViewModel == null)
                throw new ArgumentNullException(nameof(filterViewModel));

            expensesOverDateRange = new List<KeyValuePair<string, decimal>>();
            incomesOverDatesRange = new ObservableCollection<KeyValuePair<string, decimal>>();
            balanceOverDateRange = new List<KeyValuePair<DateTime, decimal>>();
            expensesOverRemiteeInDateRange = new List<KeyValuePair<string, decimal>>();
            expensesOverRemiteeGroupsInDateRange = new List<KeyValuePair<string, decimal>>();
            expensesInfoOverDateRange = new List<KeyValuePair<string, string>>();
            incomesInfoOverDateRange = new List<KeyValuePair<string, string>>();
            summary = String.Empty;
            transactionsAccounts = new List<string>();

            buchungstextOverDateRange = filterViewModel.BuchungstextValues;
            transactionsAccountsObsCollBoolTextCouple = filterViewModel.UserAccounts;

            expensesAtDate = new List<KeyValuePair<string, decimal>>();
            dates4RemiteeOverDateRange = new List<KeyValuePair<string, decimal>>();

            expenceBeneficiary4CategoryOverDateRange = new List<KeyValuePair<string, decimal>>();
            expensesOverCategory = new List<KeyValuePair<string, decimal>>();

            // Set static instance for legacy GetInstance() calls during transition
            _instance = this;
        }

        /// <summary>
        /// Gets the singleton instance. Prefer constructor injection over this method.
        /// </summary>
        public static ResponseModel GetInstance()
        {
            return _instance ?? throw new InvalidOperationException("ResponseModel not initialized. Use DI container.");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler ViewPropertyChanged;

        private List<KeyValuePair<string, decimal>> expensesOverDateRange;
        private ObservableCollection<KeyValuePair<string, decimal>> incomesOverDatesRange;
        private List<KeyValuePair<DateTime, decimal>> balanceOverDateRange;
        private List<KeyValuePair<string, decimal>> expensesOverRemiteeInDateRange;
        private List<KeyValuePair<string, decimal>> expensesOverRemiteeGroupsInDateRange;
        private List<KeyValuePair<string, string>> expensesInfoOverDateRange;
        private List<KeyValuePair<string, string>> incomesInfoOverDateRange;
        private string summary;
        private List<string> transactionsAccounts;
        private ObservableCollection<BoolTextCouple> buchungstextOverDateRange;
        private ObservableCollection<BoolTextCouple> transactionsAccountsObsCollBoolTextCouple;

        private List<KeyValuePair<string, decimal>> expensesAtDate;
        private List<KeyValuePair<string, decimal>> dates4RemiteeOverDateRange;

        private List<KeyValuePair<string, decimal>> expenceBeneficiary4CategoryOverDateRange;
        private List<KeyValuePair<string, decimal>> expensesOverCategory;

        private bool updateDataRequired = false;

        public List<KeyValuePair<string, string>> IncomesInfoOverDateRange
        {
            get => incomesInfoOverDateRange;
            set
            {
                if (incomesInfoOverDateRange != value)
                {
                    incomesInfoOverDateRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<KeyValuePair<string, decimal>> IncomesOverDatesRange
        {
            get => incomesOverDatesRange;
            set
            {
                if (incomesOverDatesRange != value)
                {
                    incomesOverDatesRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }

        public List<KeyValuePair<DateTime, decimal>> BalanceOverDateRange
        {
            get => balanceOverDateRange;
            set
            {
                if (balanceOverDateRange != value)
                {
                    balanceOverDateRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }

        public List<KeyValuePair<string, decimal>> ExpensesOverDateRange
        {
            get => expensesOverDateRange;
            set
            {
                if (expensesOverDateRange != value)
                {
                    expensesOverDateRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }
        public List<KeyValuePair<string, string>> ExpensesInfoOverDateRange
        {
            get => expensesInfoOverDateRange;
            set
            {
                if (expensesInfoOverDateRange != value)
                {
                    expensesInfoOverDateRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }
        public List<KeyValuePair<string, decimal>> ExpensesOverRemiteeInDateRange
        {
            get => expensesOverRemiteeInDateRange;
            set
            {
                if (expensesOverRemiteeInDateRange != value)
                {
                    expensesOverRemiteeInDateRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }
        public List<KeyValuePair<string, decimal>> ExpensesOverRemiteeGroupsInDateRange
        {
            get => expensesOverRemiteeGroupsInDateRange;
            set
            {
                if (expensesOverRemiteeGroupsInDateRange != value)
                {
                    expensesOverRemiteeGroupsInDateRange = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }

        public string Summary
        {
            get => summary;
            set
            {
                if (summary != value)
                {
                    summary = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> TransactionsAccounts
        {
            get => transactionsAccounts;
            set
            {
                if (transactionsAccounts != value)
                {
                    transactionsAccounts = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<BoolTextCouple> TransactionsAccountsObsCollBoolTextCouple
        {
            get => transactionsAccountsObsCollBoolTextCouple;
            set
            {
                if (transactionsAccountsObsCollBoolTextCouple != value)
                {
                    transactionsAccountsObsCollBoolTextCouple = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<BoolTextCouple> BuchungstextOverDateRange
        {
            get => buchungstextOverDateRange;
            set
            {
                if (buchungstextOverDateRange != value)
                {
                    buchungstextOverDateRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<KeyValuePair<string, decimal>> ExpensesAtDate
        {
            get => expensesAtDate;
            set
            {
                if (expensesAtDate != value)
                {
                    expensesAtDate = value;
                    OnViewPropertyChanged();
                }
            }
        }

        public List<KeyValuePair<string, decimal>> Dates4RemiteeOverDateRange
        {
            get => dates4RemiteeOverDateRange;
            set
            {
                if (dates4RemiteeOverDateRange != value)
                {
                    dates4RemiteeOverDateRange = value;
                    OnViewPropertyChanged();
                }
            }
        }

        public List<KeyValuePair<string, decimal>> ExpenceBeneficiary4CategoryOverDateRange
        {
            get => expenceBeneficiary4CategoryOverDateRange;
            set
            {
                if (expenceBeneficiary4CategoryOverDateRange != value)
                {
                    expenceBeneficiary4CategoryOverDateRange = value;
                    OnViewPropertyChanged();
                }
            }
        }

        public List<KeyValuePair<string, decimal>> ExpensesOverCategory
        {
            get => expensesOverCategory;
            set
            {
                if (expensesOverCategory != value)
                {
                    expensesOverCategory = value;
                    updateDataRequired = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool UpdateDataRequired { 
            get => updateDataRequired;
            set 
            { 
                if(updateDataRequired != value)
                {
                    updateDataRequired = value;
                    OnPropertyChanged();
                }               
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnViewPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ViewPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
