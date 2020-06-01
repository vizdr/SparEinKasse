 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WpfApplication1;
using System.Collections.ObjectModel;
using System.Threading;
using WpfApplication1.DTO;

namespace WpfApplication1
{
    class ChartsPresenter
    {
        private IViewCharts _viewChC;
        private IViewFilters _viewFilters;
        private IBuisenessLogic bl;
        private List<KeyValuePair<string, decimal>> dataSourceExpensesOverRemitee;
        public delegate List<KeyValuePair<string, decimal>> DisplayHandlerDec();
        public delegate List<KeyValuePair<string, string>> DisplayHandlerString();
        public delegate List<KeyValuePair<DateTime, decimal>> DisplayHandlerDTimeDecimal();
        DisplayHandlerDec handlerDecimalIncomes;
        DisplayHandlerString handlerStringOverview;
        DisplayHandlerDTimeDecimal handlerDTimeDecimalBalance;
        public static FilterParams FilterValues {get; private set;}
        private DataRequest request;

       

        public ChartsPresenter(IViewCharts viewChC, DataRequest request, IBuisenessLogic bl)
        {
            this.request = request;
            _viewChC = viewChC;          
            this.bl = bl;
            _viewChC.BeginDate = request.BeginDate;
            _viewChC.EndDate = request.EndDate;
            _viewChC.OnDateIntervalChanged += delegate { this.Initialaze(); };
            handlerDecimalIncomes = new DisplayHandlerDec(GetIncomes);
            handlerDTimeDecimalBalance = new DisplayHandlerDTimeDecimal(GetBalance);
        }

        public ChartsPresenter(IViewCharts viewChC)
            : this(viewChC, new DataRequest(), new BuisenessLogicSSKA()) 
        {

        }

        public void Initialaze()
        {
            WindowProgrBar progrBar = new WindowProgrBar();
            progrBar.Show();

            handlerStringOverview = new DisplayHandlerString(GetIncomesOverview);
            request.BeginDate = _viewChC.BeginDate;
            request.EndDate = _viewChC.EndDate;

            IAsyncResult resultObjBalance = handlerDTimeDecimalBalance.BeginInvoke(null, null);
                IAsyncResult resultObjOverview = handlerStringOverview.BeginInvoke(null, null);
                    _viewChC.Summary = bl.GetSummary(request);
                    IAsyncResult resultObjIncomes = handlerDecimalIncomes.BeginInvoke(null, null);      
                        InitializeExpencsesOverRemitee();
                _viewChC.IncomsOverview = handlerStringOverview.EndInvoke(resultObjOverview);
                        handlerStringOverview -= GetIncomesOverview;
                        handlerStringOverview += GetExpensesOverview;                  
                    _viewChC.Incomes = handlerDecimalIncomes.EndInvoke(resultObjIncomes);               
                resultObjOverview = handlerStringOverview.BeginInvoke(null, null);
                    _viewChC.Expenses = ConvertToDatesList(bl.GetExpensesOverDateRange(request));
                    _viewChC.Accounts = bl.GetTransactionsAccounts(request);
                    _viewChC.RemittieeGroups = bl.GetExpensesOverRemiteeGroupsInDateRange(request);
                _viewChC.ExpensesOverview = handlerStringOverview.EndInvoke(resultObjOverview);
            _viewChC.Balance = handlerDTimeDecimalBalance.EndInvoke(resultObjBalance);

            progrBar.Close();         
        }       

        private List<KeyValuePair<string, decimal>> GetIncomes()
        {
            return bl.GetIncomesOverDatesRange(request);           
        }

        private List<KeyValuePair<string, string>> GetExpensesOverview()
        {
            return bl.GetExpensesInfoOverDateRange(request);
        }

        private List<KeyValuePair<string, string>> GetIncomesOverview()
        {
            return bl.GetIncomesInfoOverDateRange(request);
        }

        private List<KeyValuePair<DateTime, decimal>> GetBalance()
        {
            return bl.GetBalanceOverDateRange(request); 
        }

        private void InitializeExpencsesOverRemitee()
        {
            this.dataSourceExpensesOverRemitee = bl.GetExpensesOverRemiteeInDateRange(request);
            _viewChC.Remitties = this.dataSourceExpensesOverRemitee;
            _viewChC.AxeRemittiesExpencesMaxValue = CalculateMaxValue(dataSourceExpensesOverRemitee);
        }

        public void ReloadXml()
        {
            bl.UpdateData();
        }

        #region Filters
        public void InitializeFilters(FilterParams FilterValues)
        {
            if (FilterValues == null)
            {
                _viewFilters.BuchungstextValues = bl.GetBuchungstextOverDateRange(request);
                _viewFilters.UserAccounts = bl.GetTransactionsAccountsObsCollBoolTextCouple(request);
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
            this.dataSourceExpensesOverRemitee = bl.GetExpensesOverRemiteeInDateRange(request);
            _viewChC.Remitties = this.dataSourceExpensesOverRemitee;
            _viewChC.AxeRemittiesExpencesMaxValue = CalculateMaxValue(dataSourceExpensesOverRemitee);
            _viewChC.Expenses = ConvertToDatesList(bl.GetExpensesOverDateRange(request));
            _viewChC.ExpensesOverview = bl.GetExpensesInfoOverDateRange(request);
            _viewChC.Accounts = bl.GetTransactionsAccounts(request);
            _viewChC.Incomes = bl.GetIncomesOverDatesRange(request);
            _viewChC.IncomsOverview = bl.GetIncomesInfoOverDateRange(request);
            _viewChC.RemittieeGroups = bl.GetExpensesOverRemiteeGroupsInDateRange(request);
            _viewChC.Balance = bl.GetBalanceOverDateRange(request);
            _viewChC.Summary = bl.GetSummary(request);

        }

        public IViewFilters ViewFilters
        {
            get { return _viewFilters; }
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

    }
}
