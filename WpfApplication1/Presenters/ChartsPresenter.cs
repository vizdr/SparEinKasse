using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WpfApplication1.DTO;
using System.ComponentModel;

namespace WpfApplication1
{
    class ChartsPresenter
    {
        private IViewCharts _viewChC;
        private IViewFilters _viewFilters;
        private IBuisenessLogic bl;
        private DataRequest request;
        private ResponseModel chartModel;
        private List<KeyValuePair<string, decimal>> dataSourceExpensesOverRemitee;
        public static FilterParams FilterValues { get; private set; }

        public ChartsPresenter(IViewCharts viewChC, IBuisenessLogic bl)
        {
            this.bl = bl;
            this.request = bl.Request;
            _viewChC = viewChC;            
            chartModel = bl.ResponseModel;
            _viewChC.BeginDate = request.BeginDate;
            _viewChC.EndDate = request.EndDate;
            _viewChC.OnDateIntervalChanged += delegate { Initialaze(); };
            chartModel.PropertyChanged += ReactOnPropertyChange;
            chartModel.ViewPropertyChanged += ReactOnViewPropertyChange;
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
            request.BeginDate = _viewChC.BeginDate;
            request.EndDate = _viewChC.EndDate;
            Thread.Sleep(120);
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
                    _viewChC.Expenses = ConvertToDatesList(chartModel.ExpensesOverDateRange);
                    break;
                case "IncomesOverDatesRange":
                    _viewChC.Incomes = chartModel.IncomesOverDatesRange;
                    break;
                case "BalanceOverDateRange":
                    _viewChC.Balance = chartModel.BalanceOverDateRange;
                    break;
                case "Summary":
                    _viewChC.Summary = chartModel.Summary;
                    break;
                case "ExpensesOverRemiteeInDateRange":
                    InitializeExpencsesOverRemitee();
                    break;
                case "IncomesInfoOverDateRange":
                    _viewChC.IncomsOverview = chartModel.IncomesInfoOverDateRange;
                    break;
                case "ExpensesInfoOverDateRange":
                    _viewChC.ExpensesOverview = chartModel.ExpensesInfoOverDateRange;
                    break;
                case "TransactionsAccounts":
                    _viewChC.Accounts = chartModel.TransactionsAccounts;
                    break;
                case "ExpensesOverRemiteeGroupsInDateRange":
                    _viewChC.RemittieeGroups = chartModel.ExpensesOverRemiteeGroupsInDateRange;
                    break;
                case "Balance":
                    _viewChC.Balance = chartModel.BalanceOverDateRange;
                    break;
                case "BuchungstextOverDateRange":
                    _viewFilters.BuchungstextValues = chartModel.BuchungstextOverDateRange;
                    break;
                case "TransactionsAccountsObsCollBoolTextCouple":
                    _viewFilters.UserAccounts = chartModel.TransactionsAccountsObsCollBoolTextCouple;
                    break;
                default:
                    { };
                    break;
            }
        }

        // never called because currently set xx is not in Use 
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

        private void InitializeExpencsesOverRemitee()
        {
            dataSourceExpensesOverRemitee = chartModel.ExpensesOverRemiteeInDateRange;
            _viewChC.Remitties = this.dataSourceExpensesOverRemitee;
            _viewChC.AxeRemittiesExpencesMaxValue = CalculateMaxValue(dataSourceExpensesOverRemitee);
        }

        public void ReloadXml()
        {
            request.DataBankUpdating = true;
        }

        #region Filters
        public void InitializeFilters(FilterParams FilterValues)
        {
            if (FilterValues == null)
            {
                FilterValues = new FilterParams(_viewFilters.ExpenciesLessThan, _viewFilters.ExpenciesMoreThan,
                _viewFilters.IncomesLessThan, _viewFilters.IncomesMoreThan, _viewFilters.ToFind, _viewFilters.BuchungstextValues, _viewFilters.UserAccounts);
                request.Filters = FilterValues;
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

        private void ApplyFilters()
        {
            FilterValues = new FilterParams(_viewFilters.ExpenciesLessThan, _viewFilters.ExpenciesMoreThan,
                _viewFilters.IncomesLessThan, _viewFilters.IncomesMoreThan, _viewFilters.ToFind, _viewFilters.BuchungstextValues, _viewFilters.UserAccounts);
            request.Filters = FilterValues;
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

        public string GetExpencesAtDate(DateTime inDate)
        {
            string resString = "";
            request.AtDate = inDate;
            List<KeyValuePair<string, decimal>> resList = chartModel.ExpensesAtDate;
            foreach (KeyValuePair<string, decimal> el in resList)
            {
                resString += el.Key + ": " + el.Value.ToString() + "\n";
            }
            return resString.Trim();
        }

        public string GetDates4Remitee(string remittee)
        {
            string resString = "";
            request.SelectedRemittee = remittee;
            List<KeyValuePair<string, decimal>> resList = chartModel.Dates4RemiteeOverDateRange;
            foreach (KeyValuePair<string, decimal> el in resList)
            {
                resString += el.Key + ": " + el.Value.ToString() + "\n";
            }
            return resString.Trim();
        }
    }
}
