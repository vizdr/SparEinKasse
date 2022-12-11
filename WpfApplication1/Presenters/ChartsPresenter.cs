using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.ComponentModel;

namespace WpfApplication1
{
    class ChartsPresenter
    {
        private readonly IViewCharts _viewCharts;
        private IViewFilters _viewFilters;
        private readonly IBuisenessLogic bl;
        public static FilterParams FilterValues { get; private set; }

        public ChartsPresenter(IViewCharts viewChart, IBuisenessLogic bl)
        {
            this.bl = bl ?? throw new ArgumentNullException(nameof(bl));
            _viewCharts = viewChart ?? throw new ArgumentNullException(nameof(viewChart));            
            _viewCharts.BeginDate = bl.Request.BeginDate;
            _viewCharts.EndDate = bl.Request.EndDate;
            bl.ResponseModel.PropertyChanged += ReactOnPropertyChange;
            bl.ResponseModel.ViewPropertyChanged += ReactOnViewPropertyChange;
        }

        public ChartsPresenter(IViewCharts viewChC)
            : this(viewChC, BuisenessLogicSSKA.GetInstance())
        {
        }

        static ChartsPresenter()
        {
        }

        // Initiate update of data model by change of xxDate property for DataRequest 
        public void Initialaze()
        {
            bl.Request.BeginDate = _viewCharts.BeginDate;
            bl.Request.EndDate = _viewCharts.EndDate;
            Thread.Sleep(150);
        }

        public void FinalizeChP()
        {
            bl.FinalizeBL();
        }

        public void ReactOnPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            string s = e.PropertyName;
            switch (s)
            {
                case "ExpensesOverDateRange":
                    _viewCharts.Expenses = ConvertToDatesList(bl.ResponseModel.ExpensesOverDateRange);
                    break;
                case "IncomesOverDatesRange":
                    _viewCharts.Incomes = bl.ResponseModel.IncomesOverDatesRange;
                    break;
                case "BalanceOverDateRange":
                    _viewCharts.Balance = bl.ResponseModel.BalanceOverDateRange;
                    break;
                case "Summary":
                    _viewCharts.Summary = bl.ResponseModel.Summary;
                    break;
                case "ExpensesOverRemiteeInDateRange":
                    InitializeExpencsesOverRemitee(bl.ResponseModel.ExpensesOverRemiteeInDateRange);
                    break;
                case "IncomesInfoOverDateRange":
                    _viewCharts.IncomsOverview = bl.ResponseModel.IncomesInfoOverDateRange;
                    break;
                case "ExpensesInfoOverDateRange":
                    _viewCharts.ExpensesOverview = bl.ResponseModel.ExpensesInfoOverDateRange;
                    break;
                case "TransactionsAccounts":
                    _viewCharts.Accounts = bl.ResponseModel.TransactionsAccounts;
                    break;
                case "ExpensesOverRemiteeGroupsInDateRange":
                    _viewCharts.RemittieeGroups = bl.ResponseModel.ExpensesOverRemiteeGroupsInDateRange;
                    break;
                case "Balance":
                    _viewCharts.Balance = bl.ResponseModel.BalanceOverDateRange;
                    break;
                case "BuchungstextOverDateRange":
                    _viewFilters.BuchungstextValues = bl.ResponseModel.BuchungstextOverDateRange;
                    break;
                case "TransactionsAccountsObsCollBoolTextCouple":
                    _viewFilters.UserAccounts = bl.ResponseModel.TransactionsAccountsObsCollBoolTextCouple;
                    break;
                case "ExpensesOverCategory":
                    InitializeExpencsesOverCategory(bl.ResponseModel.ExpensesOverCategory);
                    break;
                default:
                    { };
                    break;
            }
        }

        // draft, currently set for viewProperty xx occurs via mouse up event handlers 
        public void ReactOnViewPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            String s = e.PropertyName;
            switch (s)
            {
                case "ExpensesAtDate":
                    //chartModel.ExpensesAtDate = ;
                    break;
                case "Dates4RemiteeOverDateRange":
                    //chartModel.ExpensesAtDate = ;
                    break;
                default:
                    { };
                    break;
            }
        }

        private void InitializeExpencsesOverRemitee(List<KeyValuePair<string, decimal>> dataSourceExpensesOverRemitee)
        {
            _viewCharts.Remitties = dataSourceExpensesOverRemitee;
            _viewCharts.AxeRemittiesExpencesMaxValue = CalculateMaxValue(dataSourceExpensesOverRemitee);
        }

        private void InitializeExpencsesOverCategory(List<KeyValuePair<string, decimal>> dataSourceExpensesOverCategory)
        {
            _viewCharts.ExpensesCategory = dataSourceExpensesOverCategory;
            _viewCharts.AxeExpencesCategoryMaxValue = CalculateMaxValue(dataSourceExpensesOverCategory);
        }

        public void ReloadXml()
        {
            bl.Request.DataBankUpdating = true; // to emit event
        }

        #region Filters
        public void InitializeFilters(FilterParams FilterValues)
        {
            if (FilterValues == null)
            {
                FilterValues = new FilterParams(_viewFilters.ExpenciesLessThan, _viewFilters.ExpenciesMoreThan,
                _viewFilters.IncomesLessThan, _viewFilters.IncomesMoreThan, _viewFilters.ToFind, _viewFilters.BuchungstextValues, _viewFilters.UserAccounts);
                bl.Request.Filters = FilterValues;
            }
            else
            {
                _viewFilters.BuchungstextValues = FilterValues.BuchungstextValues;
                _viewFilters.UserAccounts = FilterValues.Accounts;
                _viewFilters.ExpenciesLessThan = FilterValues.ExpenciesLessThan;
                _viewFilters.ExpenciesMoreThan = FilterValues.ExpenciesMoreThan;
                _viewFilters.IncomesLessThan = FilterValues.IncomesLessThan;
                _viewFilters.IncomesMoreThan = FilterValues.IncomesMoreThan;
                _viewFilters.ToFind = FilterValues.ToFind;
            }
        }

        private void RegisterFiltersHandlers()
        {
            _viewFilters.OnApplyFilter += delegate { ApplyFilters(); };
            _viewFilters.OnResetFilters += delegate { ResetFilters(); };
        }

        private void ResetFilters()
        {
            _viewFilters.ExpenciesLessThan = _viewFilters.ExpenciesMoreThan = _viewFilters.IncomesLessThan =
                _viewFilters.IncomesMoreThan = _viewFilters.ToFind = String.Empty;
            if (FilterValues != null)
            {
                FilterValues.ResetValues();
            }
            InitializeFilters(null);
        }

        // apply if selection of params is completed
        private void ApplyFilters()
        {
            FilterValues = new FilterParams(_viewFilters.ExpenciesLessThan, _viewFilters.ExpenciesMoreThan,
                _viewFilters.IncomesLessThan, _viewFilters.IncomesMoreThan, _viewFilters.ToFind, _viewFilters.BuchungstextValues, _viewFilters.UserAccounts);
            bl.Request.Filters = FilterValues;
        }

        public IViewFilters ViewFilters
        {
            get => _viewFilters;
            set
            {
                _viewFilters = value;
                RegisterFiltersHandlers();
            }
        }
        #endregion

        private List<KeyValuePair<DateTime, decimal>> ConvertToDatesList(List<KeyValuePair<string, decimal>> src)
        {
            List<KeyValuePair<DateTime, decimal>> res;
            res = src.ConvertAll<KeyValuePair<DateTime, decimal>>(
                 inp => new KeyValuePair<DateTime, decimal>(DateTime.Parse(inp.Key).Date, inp.Value));
            return res;
        }

        public decimal CalculateMaxValue(List<KeyValuePair<string, decimal>> source)
        {
            IEnumerable<decimal> maxVal =
                    from el in source
                    select el.Value;
            return maxVal.Count() > 0 ? maxVal.Max() + 2 : 0m;
        }

        // for the defined in xaml mouse up event handlers, presentationFramework - external dll
        public string GetExpencesAtDate(DateTime inDate)
        {
            string resString = "";
            bl.Request.AtDate = inDate;
            List<KeyValuePair<string, decimal>> resList = bl.ResponseModel.ExpensesAtDate;
            foreach (KeyValuePair<string, decimal> el in resList)
            {
                resString += el.Key + ": " + el.Value.ToString() + "\n";
            }
            return resString.Trim();
        }
        
        public string GetDates4Remitee(string remittee)
        {
            string resString = "";
            bl.Request.SelectedRemittee = remittee;
            List<KeyValuePair<string, decimal>> resList = bl.ResponseModel.Dates4RemiteeOverDateRange;
            foreach (KeyValuePair<string, decimal> el in resList)
            {
                resString += el.Key + ": " + el.Value.ToString() + "\n";
            }
            return resString.Trim();
        }

        public string GetDateBeneficiary(string category)
        {
            string resString = "";
            bl.Request.SelectedCategory = category;
            List<KeyValuePair<string, decimal>> resList = bl.ResponseModel.ExpenceBeneficiary4CategoryOverDateRange;
            foreach (KeyValuePair<string, decimal> el in resList)
            {
                resString += el.Key + ": " + el.Value.ToString() + "\n";
            }
            return resString.Trim();
        }
    }
}
