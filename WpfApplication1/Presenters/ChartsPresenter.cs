using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace WpfApplication1
{
    class ChartsPresenter
    {
        private readonly IViewCharts _viewCharts;
        private IViewFilters _viewFilters;
        private readonly IBusinessLogic bl;
        private readonly FilterViewModel _filterViewModel;

        private readonly BusinessLogicSSKA _businessLogic;

        public ChartsPresenter(IViewCharts viewChart, BusinessLogicSSKA businessLogic, FilterViewModel filterViewModel)
        {
            _businessLogic = businessLogic ?? throw new ArgumentNullException(nameof(businessLogic));
            bl = businessLogic;
            _viewCharts = viewChart ?? throw new ArgumentNullException(nameof(viewChart));
            _filterViewModel = filterViewModel ?? throw new ArgumentNullException(nameof(filterViewModel));

            _viewCharts.BeginDate = businessLogic.Request.TimeSpan.Item1.Date;
            _viewCharts.EndDate = businessLogic.Request.TimeSpan.Item2.Date;

            businessLogic.ResponseModel.PropertyChanged += OnResponseModelPropertyChanged;
        }

        public void Initialize()
        {
            var newTimeSpan = new Tuple<DateTime, DateTime>(_viewCharts.BeginDate, _viewCharts.EndDate);
            bl.Request.TimeSpan = newTimeSpan;
        }

        [Obsolete("Use Initialize() instead")]
        public void Initialaze() => Initialize();

        public void FinalizeChP()
        {
            // Unsubscribe from events to prevent updates to closed window
            _businessLogic.ResponseModel.PropertyChanged -= OnResponseModelPropertyChanged;
            bl.FinalizeBL();
        }

        private void OnResponseModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(bl.ResponseModel.IsDataReady):
                    FeedUpdatedData();
                    break;

                case nameof(bl.ResponseModel.Summary):
                    _viewCharts.Summary = bl.ResponseModel.Summary;
                    break;

                case nameof(bl.ResponseModel.BuchungstextOverDateRange):
                    if (_viewFilters != null)
                        _viewFilters.BuchungstextValues = bl.ResponseModel.BuchungstextOverDateRange;
                    break;

                case nameof(bl.ResponseModel.TransactionsAccountsObsCollBoolTextCouple):
                    if (_viewFilters != null)
                        _viewFilters.UserAccounts = bl.ResponseModel.TransactionsAccountsObsCollBoolTextCouple;
                    break;
            }
        }

        private void FeedUpdatedData()
        {
            var dispatcher = (_viewCharts as Window)?.Dispatcher ?? Application.Current?.Dispatcher;

            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(FeedUpdatedDataCore);
                return;
            }
            FeedUpdatedDataCore();
        }

        private void FeedUpdatedDataCore()
        {
            _viewCharts.Expenses = ConvertToDatesList(bl.ResponseModel.ExpensesOverDateRange);
            _viewCharts.Incomes = bl.ResponseModel.IncomesOverDatesRange;
            _viewCharts.Remitties = bl.ResponseModel.ExpensesOverRemiteeInDateRange;
            _viewCharts.AxeRemittiesExpencesMaxValue = CalculateMaxValue(bl.ResponseModel.ExpensesOverRemiteeInDateRange);
            _viewCharts.IncomsOverview = bl.ResponseModel.IncomesInfoOverDateRange;
            _viewCharts.ExpensesOverview = bl.ResponseModel.ExpensesInfoOverDateRange;
            _viewCharts.Accounts = bl.ResponseModel.TransactionsAccounts;
            _viewCharts.RemittieeGroups = bl.ResponseModel.ExpensesOverRemiteeGroupsInDateRange;
            _viewCharts.Balance = bl.ResponseModel.BalanceOverDateRange;
            _viewCharts.AxeExpencesCategoryMaxValue = CalculateMaxValue(bl.ResponseModel.ExpensesOverCategory);
            _viewCharts.ExpensesCategory = bl.ResponseModel.ExpensesOverCategory;
        }

        public void ReloadXml()
        {
            bl.Request.DataBankUpdating = true;
        }

        #region Filters

        public IViewFilters ViewFilters
        {
            get => _viewFilters;
            set => _viewFilters = value;
        }

        public void InitializeFilters(IViewFilters viewFilters)
        {
            if (!bl.Request.Filters.IsFilterPrepared())
                bl.Request.Filters = _filterViewModel;
            else
                _filterViewModel.SetViewFilters(viewFilters);
        }

        public void ResetFilters()
        {
            _viewFilters.ExpenciesLessThan = string.Empty;
            _viewFilters.ExpenciesMoreThan = string.Empty;
            _viewFilters.IncomesLessThan = string.Empty;
            _viewFilters.IncomesMoreThan = string.Empty;
            _viewFilters.ToFind = string.Empty;

            foreach (var val in _viewFilters.BuchungstextValues)
                val.IsSelected = true;

            foreach (var val in _viewFilters.UserAccounts)
                val.IsSelected = true;

            _filterViewModel.SetViewFilters(ViewFilters);
            bl.Request.Filters = _filterViewModel;
        }

        public void ApplyFilters()
        {
            _filterViewModel.SetViewFilters(ViewFilters);
            bl.Request.Filters = _filterViewModel;
        }

        #endregion

        #region Detail Data Getters

        public string GetExpensesAtDate(DateTime date)
        {
            bl.Request.AtDate = date;
            return FormatKeyValueList(bl.ResponseModel.ExpensesAtDate);
        }

        [Obsolete("Use GetExpensesAtDate instead")]
        public string GetExpencesAtDate(DateTime inDate) => GetExpensesAtDate(inDate);

        public string GetDates4Remitee(string remittee)
        {
            bl.Request.SelectedRemittee = remittee;
            return FormatKeyValueList(bl.ResponseModel.Dates4RemiteeOverDateRange);
        }

        public string GetDateBeneficiary(string category)
        {
            bl.Request.SelectedCategory = category;
            return FormatKeyValueList(bl.ResponseModel.ExpenseBeneficiary4CategoryOverDateRange);
        }

        #endregion

        #region Helper Methods

        private static List<KeyValuePair<DateTime, decimal>> ConvertToDatesList(List<KeyValuePair<string, decimal>> src)
        {
            if (src == null)
                return new List<KeyValuePair<DateTime, decimal>>();

            return src.ConvertAll(kvp => new KeyValuePair<DateTime, decimal>(DateTime.Parse(kvp.Key).Date, kvp.Value));
        }

        private static decimal CalculateMaxValue(List<KeyValuePair<string, decimal>> source)
        {
            if (source == null || source.Count == 0)
                return 0m;

            return source.Max(kvp => kvp.Value) + 2;
        }

        private static string FormatKeyValueList(List<KeyValuePair<string, decimal>> list)
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            return string.Join("\n", list.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        #endregion
    }
}
