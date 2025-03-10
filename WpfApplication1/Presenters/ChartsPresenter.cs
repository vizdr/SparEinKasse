using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace WpfApplication1
{
    class ChartsPresenter
    {
        private readonly IViewCharts _viewCharts;
        private IViewFilters _viewFilters;
        private readonly IBuisenessLogic bl;

        public ChartsPresenter(IViewCharts viewChart, BuisenessLogicSSKA bl)
        {
            this.bl = bl ?? throw new ArgumentNullException(nameof(bl));
            _viewCharts = viewChart ?? throw new ArgumentNullException(nameof(viewChart));

            _viewCharts.BeginDate = bl.Request.TimeSpan.Item1.Date;
            _viewCharts.EndDate = bl.Request.TimeSpan.Item2.Date;
            bl.ResponseModel.PropertyChanged += ReactOnPropertyChange;
            bl.ResponseModel.ViewPropertyChanged += ReactOnViewPropertyChange;
        }

        public ChartsPresenter(IViewCharts viewChC)
            : this(viewChC, BuisenessLogicSSKA.GetInstance()) {}

        static ChartsPresenter() {}

        // Initiate update of data model by change of xxDate property for DataRequest 
        public void Initialaze()
        {
            bl.Request.TimeSpan = new Tuple<DateTime, DateTime>(_viewCharts.BeginDate, _viewCharts.EndDate);           
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
                case "IncomesOverDatesRange":
                case "BalanceOverDateRange":               
                case "ExpensesOverRemiteeInDateRange":
                case "IncomesInfoOverDateRange":
                case "ExpensesInfoOverDateRange":
                case "TransactionsAccounts":
                case "ExpensesOverRemiteeGroupsInDateRange":
                case "Balance":
                case "ExpensesOverCategory":
                    break;
                case "BuchungstextOverDateRange":
                    _viewFilters.BuchungstextValues = bl.ResponseModel.BuchungstextOverDateRange; 
                    break;
                case "TransactionsAccountsObsCollBoolTextCouple":
                    _viewFilters.UserAccounts = bl.ResponseModel.TransactionsAccountsObsCollBoolTextCouple;
                    break;
                case "Summary":
                    _viewCharts.Summary = bl.ResponseModel.Summary;
                    break;
           
                case "UpdateDataRequired":
                    feedUpdatedData();
                    break;
                default:
                    { };
                    break;
            }
        }

        private void feedUpdatedData()
        { 
            _viewCharts.Expenses = ConvertToDatesList(bl.ResponseModel.ExpensesOverDateRange);
            _viewCharts.Incomes = bl.ResponseModel.IncomesOverDatesRange;
            InitializeExpencsesOverRemitee(bl.ResponseModel.ExpensesOverRemiteeInDateRange);
            _viewCharts.IncomsOverview = bl.ResponseModel.IncomesInfoOverDateRange;
            _viewCharts.ExpensesOverview = bl.ResponseModel.ExpensesInfoOverDateRange;
            _viewCharts.IncomsOverview = bl.ResponseModel.IncomesInfoOverDateRange;
            _viewCharts.Accounts = bl.ResponseModel.TransactionsAccounts;
            _viewCharts.RemittieeGroups = bl.ResponseModel.ExpensesOverRemiteeGroupsInDateRange;
            _viewCharts.Balance = bl.ResponseModel.BalanceOverDateRange;
            InitializeExpencsesOverCategory(bl.ResponseModel.ExpensesOverCategory);
        }

        // Draft, currently set for viewProperty xx occurs via mouse up event handlers 
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
            _viewCharts.AxeExpencesCategoryMaxValue = CalculateMaxValue(dataSourceExpensesOverCategory);            
            _viewCharts.ExpensesCategory = dataSourceExpensesOverCategory;           
        }

        public void ReloadXml()
        {
            bl.Request.DataBankUpdating = true; // to emit event
        }

        #region Filters
        public void InitializeFilters(IViewFilters viewFilters)
        {
            if (!bl.Request.Filters.IsFilterPrepared())
                bl.Request.Filters = FilterViewModel.GetInstance();
            else
                FilterViewModel.SetViewFilters(viewFilters);
        }

        public void ResetFilters()
        {
            _viewFilters.ExpenciesLessThan = _viewFilters.ExpenciesMoreThan = _viewFilters.IncomesLessThan =
                _viewFilters.IncomesMoreThan = _viewFilters.ToFind = String.Empty;

            foreach (BoolTextCouple val in _viewFilters.BuchungstextValues)
            {
                val.IsSelected = true;
            }
            foreach (BoolTextCouple val in _viewFilters.UserAccounts)
            {
                val.IsSelected = true;
            }

            FilterViewModel.SetViewFilters(ViewFilters);
            bl.Request.Filters = FilterViewModel.GetInstance();
        }

        // apply if selection of params is completed
        public void ApplyFilters()
        {
            FilterViewModel.SetViewFilters(ViewFilters);
            bl.Request.Filters = FilterViewModel.GetInstance();
        }

        public IViewFilters ViewFilters
        {
            get => _viewFilters;
            set
            {
                _viewFilters = value;
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

        // For the defined in xaml mouse up event handlers, presentationFramework - external dll
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
