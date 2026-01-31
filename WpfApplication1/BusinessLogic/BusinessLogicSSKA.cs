using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.Globalization;
using System.Collections.ObjectModel;
using WpfApplication1.DAL;
using WpfApplication1.DTO;
using System.ComponentModel;
using System.Windows.Threading;
using WpfApplication1.BusinessLogic;

namespace WpfApplication1
{
    public class BusinessLogicSSKA : IBusinessLogic
    {
        private readonly CsvToXmlSSKA dataGate;
        private readonly IDataSourceProvider _dataSourceProvider;
        private readonly WindowProgrBar progrBar;
        private readonly BackgroundWorker worker;
        private readonly ResponseModel responseModel;
        private static PreprocessedDataRequest preprocessedRequest;

        public ResponseModel ResponseModel => responseModel;
        public DataRequest Request { get; }

        public BusinessLogicSSKA(
            DataRequest dataRequest,
            ResponseModel responseModel,
            CsvToXmlSSKA csvToXmlSSKA,
            IDataSourceProvider dataSourceProvider)
        {
            Request = dataRequest ?? throw new ArgumentNullException(nameof(dataRequest));
            this.responseModel = responseModel ?? throw new ArgumentNullException(nameof(responseModel));
            dataGate = csvToXmlSSKA ?? throw new ArgumentNullException(nameof(csvToXmlSSKA));
            _dataSourceProvider = dataSourceProvider ?? throw new ArgumentNullException(nameof(dataSourceProvider));

            progrBar = new WindowProgrBar();
            InitializePreprocessedRequest();
            RegisterDataRequestHandlers();

            worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            UpdateDataModel();
        }

        private void RegisterDataRequestHandlers()
        {
            Request.DataRequested += (s, e) => UpdateDataModel();
            Request.FilterValuesRequested += (s, e) => FilterData();
            Request.DataBankUpdateRequested += (s, e) => UpdateData();
            Request.ViewDataRequested += (s, e) => UpdateViewData();
        }

        public void UpdateData()
        {
            if (dataGate.UpdateDataBank())
                UpdateDataModel();
        }

        public void UpdateDataModel()
        {
            PreProcessRequest(Request);

            if (!progrBar.IsVisible)
            {
                progrBar.Show();
                progrBar.pbStatus.Value = 0;
            }

            if (!worker.IsBusy)
            {
                responseModel.IsDataReady = false;
                worker.RunWorkerAsync();
            }

            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;
        }

        public void FilterData()
        {
            UpdateDataModel();
            responseModel.BuchungstextOverDateRange = GetBuchungstextOverDateRange();
            responseModel.TransactionsAccountsObsCollBoolTextCouple = GetTransactionsAccountsObsCollBoolTextCouple();
        }

        public void UpdateViewData()
        {
            if (Request.AtDate != DateTime.MinValue)
                responseModel.ExpensesAtDate = GetExpensesAtDate(Request);

            if (!string.IsNullOrEmpty(Request.SelectedRemittee))
                responseModel.Dates4RemiteeOverDateRange = GetDates4RemiteeOverDateRange(Request);

            if (!string.IsNullOrEmpty(Request.SelectedCategory))
                responseModel.ExpenseBeneficiary4CategoryOverDateRange = GetExpenseBeneficiary4CategoryOverDateRange(Request);
        }

        public void FinalizeBL()
        {
            progrBar?.Close();
        }

        #region BackgroundWorker

        private class WorkerResult
        {
            public List<KeyValuePair<string, decimal>> ExpensesOverDateRange { get; set; }
            public ObservableCollection<KeyValuePair<string, decimal>> IncomesOverDatesRange { get; set; }
            public List<KeyValuePair<DateTime, decimal>> BalanceOverDateRange { get; set; }
            public List<KeyValuePair<string, decimal>> ExpensesOverRemiteeInDateRange { get; set; }
            public List<KeyValuePair<string, decimal>> ExpensesOverRemiteeGroupsInDateRange { get; set; }
            public List<KeyValuePair<string, string>> ExpensesInfoOverDateRange { get; set; }
            public List<KeyValuePair<string, string>> IncomesInfoOverDateRange { get; set; }
            public string Summary { get; set; }
            public List<string> TransactionsAccounts { get; set; }
            public List<KeyValuePair<string, decimal>> ExpensesOverCategory { get; set; }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgWorker = (BackgroundWorker)sender;
            var result = new WorkerResult();

            try
            {
                result.ExpensesOverDateRange = GetExpensesOverDateRange();
                bgWorker.ReportProgress(10);

                result.TransactionsAccounts = GetTransactionsAccounts();
                bgWorker.ReportProgress(20);

                result.IncomesOverDatesRange = GetIncomesOverDatesRange();
                bgWorker.ReportProgress(30);

                result.BalanceOverDateRange = GetBalanceOverDateRange();
                bgWorker.ReportProgress(40);

                result.ExpensesInfoOverDateRange = GetExpensesInfoOverDateRange();
                bgWorker.ReportProgress(50);

                result.ExpensesOverRemiteeGroupsInDateRange = GetExpensesOverRemiteeGroupsInDateRange();
                bgWorker.ReportProgress(60);

                result.ExpensesOverRemiteeInDateRange = GetExpensesOverRemiteeInDateRange();
                bgWorker.ReportProgress(70);

                result.ExpensesOverCategory = GetExpensesOverCategory();
                bgWorker.ReportProgress(80);

                result.IncomesInfoOverDateRange = GetIncomesInfoOverDateRange();
                bgWorker.ReportProgress(90);

                result.Summary = GetSummary();
                bgWorker.ReportProgress(100);
            }
            catch (Exception)
            {
                // Preserve original behavior: don't crash; results may be partially filled
            }

            e.Result = result;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progrBar.pbStatus.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() => progrBar.pbStatus.Value = e.ProgressPercentage));
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var dispatcher = progrBar.Dispatcher ?? Application.Current?.Dispatcher;

            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => ApplyWorkerResult(e)));
            else
                ApplyWorkerResult(e);
        }

        private void ApplyWorkerResult(RunWorkerCompletedEventArgs e)
        {
            if (progrBar.IsVisible)
                progrBar.Hide();

            if (e.Result is WorkerResult result)
            {
                if (result.ExpensesOverDateRange != null)
                    responseModel.ExpensesOverDateRange = result.ExpensesOverDateRange;
                if (result.TransactionsAccounts != null)
                    responseModel.TransactionsAccounts = result.TransactionsAccounts;
                if (result.IncomesOverDatesRange != null)
                    responseModel.IncomesOverDatesRange = result.IncomesOverDatesRange;
                if (result.BalanceOverDateRange != null)
                    responseModel.BalanceOverDateRange = result.BalanceOverDateRange;
                if (result.ExpensesInfoOverDateRange != null)
                    responseModel.ExpensesInfoOverDateRange = result.ExpensesInfoOverDateRange;
                if (result.ExpensesOverRemiteeGroupsInDateRange != null)
                    responseModel.ExpensesOverRemiteeGroupsInDateRange = result.ExpensesOverRemiteeGroupsInDateRange;
                if (result.ExpensesOverRemiteeInDateRange != null)
                    responseModel.ExpensesOverRemiteeInDateRange = result.ExpensesOverRemiteeInDateRange;
                if (result.ExpensesOverCategory != null)
                    responseModel.ExpensesOverCategory = result.ExpensesOverCategory;
                if (result.IncomesInfoOverDateRange != null)
                    responseModel.IncomesInfoOverDateRange = result.IncomesInfoOverDateRange;
                if (result.Summary != null)
                    responseModel.Summary = result.Summary;

                responseModel.IsDataReady = true;
            }

            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;
        }

        #endregion

        #region Request Processing

        private void InitializePreprocessedRequest()
        {
            preprocessedRequest = new PreprocessedDataRequest
            {
                Accounts = new List<string>(),
                Buchungstexts = new List<string>(),
                IncomsLowestValue = decimal.Zero,
                IncomsHighestValue = decimal.MaxValue,
                ExpencesLowestValue = decimal.Zero,
                ExpencesHighestValue = decimal.MaxValue,
                BeginDate = DateTime.Now.Date.AddDays(-30),
                FinishDate = DateTime.Now.Date
            };
        }

        private static void PreProcessRequest(DataRequest request)
        {
            if (request == null)
                return;

            preprocessedRequest.AtDate = request.AtDate;
            preprocessedRequest.SelectedRemittee = request.SelectedRemittee;
            preprocessedRequest.BeginDate = request.TimeSpan.Item1;
            preprocessedRequest.FinishDate = request.TimeSpan.Item2 < request.TimeSpan.Item1
                ? request.TimeSpan.Item1
                : request.TimeSpan.Item2;

            if (request.Filters.UserAccounts.Count > 0)
            {
                if (!decimal.TryParse(request.Filters.ExpenciesLessThan, out decimal expLessThan))
                    expLessThan = decimal.MaxValue;

                decimal.TryParse(request.Filters.ExpenciesMoreThan, out decimal expMoreThan);

                if (!decimal.TryParse(request.Filters.IncomesLessThan, out decimal incomsHighestValue))
                    incomsHighestValue = decimal.MaxValue;

                decimal.TryParse(request.Filters.IncomesMoreThan, out decimal incomsLowestValue);

                preprocessedRequest.Buchungstexts.Clear();
                preprocessedRequest.Buchungstexts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.BuchungstextValues));

                preprocessedRequest.Accounts.Clear();
                preprocessedRequest.Accounts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.UserAccounts));

                preprocessedRequest.ToFind = request.Filters.ToFind;
                preprocessedRequest.ExpencesLowestValue = expMoreThan;
                preprocessedRequest.ExpencesHighestValue = expLessThan;
                preprocessedRequest.IncomsLowestValue = incomsLowestValue;
                preprocessedRequest.IncomsHighestValue = incomsHighestValue;
            }
        }

        private static List<string> ConvertObsCollBoolTextCoupleToList(ObservableCollection<BoolTextCouple> values)
        {
            if (values == null)
                return new List<string>();

            return values.Where(elem => !elem.IsSelected).Select(elem => elem.Text).ToList();
        }

        private IEnumerable<KeyValuePair<string, decimal>> ApplyRangeFilter(IEnumerable<KeyValuePair<string, decimal>> res)
        {
            return res.Where(p => p.Value >= preprocessedRequest.ExpencesLowestValue
                               && p.Value <= preprocessedRequest.ExpencesHighestValue);
        }

        #endregion

        #region Data Queries

        private decimal ConvertStringToDecimal(string src)
        {
            try
            {
                return decimal.Parse(src, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                MessageBox.Show(e.Message,
                    "Unable to parse string in the currency. Try to change the currency separator symbol in Windows Regional settings and reload the data.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return 0m;
            }
        }

        private IEnumerable<XElement> GetFilteredTransactions()
        {
            return _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                .Where(r => DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                         && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                         && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                         && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value));
        }

        protected List<KeyValuePair<string, decimal>> GetExpensesOverDateRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum(n => -ConvertStringToDecimal(n)));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpensesOverDateRange**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeInDateRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum(n => -ConvertStringToDecimal(n)));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpencesOverRemiteeInDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, string>> GetExpensesInfoOverDateRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= 0
                          && MatchesSearchFilter(r)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, string>(
                        r.Element(Config.WertDatumField).Value,
                        $" ** {r.Element(Config.BeguenstigterField).Value}" +
                        $" ** {TruncateString(r.Element(Config.VerwendZweckField).Value, 120)}" +
                        $" ** {ConvertStringToDecimal(r.Element(Config.BetragField).Value)}");

                return res.ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpensesInfoOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeGroupsInDateRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= 0
                    group r.Element(Config.BetragField).Value by r.Element(Config.BuchungsTextField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum(n => -ConvertStringToDecimal(n)));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpencesOverRemiteeInDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, decimal>> GetExpensesAtDate(DataRequest request)
        {
            try
            {
                var res =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) == request.AtDate
                          && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                          && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                          && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum(n => -ConvertStringToDecimal(n)));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpensesAtDate**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, string>> GetIncomesInfoOverDateRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    let amount = ConvertStringToDecimal(r.Element(Config.BetragField).Value)
                    where amount >= preprocessedRequest.IncomsLowestValue
                          && amount <= preprocessedRequest.IncomsHighestValue
                          && MatchesSearchFilter(r)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, string>(
                        r.Element(Config.WertDatumField).Value,
                        $" ** {r.Element(Config.BeguenstigterField).Value}" +
                        $" ** {TruncateString(r.Element(Config.VerwendZweckField).Value, 120)}" +
                        $" ** {amount}");

                return res.ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetIncomesInfoOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected ObservableCollection<KeyValuePair<string, decimal>> GetIncomesOverDatesRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= 0
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key.Substring(5), g.Sum(n => ConvertStringToDecimal(n)));

                var filtered = res.Where(p => p.Value >= preprocessedRequest.IncomsLowestValue
                                           && p.Value <= preprocessedRequest.IncomsHighestValue);

                return new ObservableCollection<KeyValuePair<string, decimal>>(filtered.ToList());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetIncomesOverDatesRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<DateTime, decimal>> GetBalanceOverDateRange()
        {
            var resultedList = new List<KeyValuePair<DateTime, decimal>>();
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<DateTime, decimal>(DateTime.Parse(g.Key).Date, g.Sum(n => ConvertStringToDecimal(n)));

                var inputList = res.ToList();
                using (var inputEnumerator = inputList.GetEnumerator())
                {
                    decimal akku = 0m;
                    if (inputEnumerator.MoveNext())
                    {
                        for (var currDate = preprocessedRequest.BeginDate;
                             !preprocessedRequest.FinishDate.Date.Equals(currDate.Date.AddDays(-1));
                             currDate = currDate.Date.AddDays(1))
                        {
                            if (inputEnumerator.Current.Key.Equals(currDate))
                            {
                                akku += inputEnumerator.Current.Value;
                                resultedList.Add(new KeyValuePair<DateTime, decimal>(inputEnumerator.Current.Key, akku));
                                inputEnumerator.MoveNext();
                            }
                            else
                            {
                                resultedList.Add(new KeyValuePair<DateTime, decimal>(currDate, akku));
                            }
                        }
                    }
                }
                return resultedList;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetBalanceOverDateRange**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected string GetSummary()
        {
            string result = $"Total: {preprocessedRequest.BeginDate:d} - {preprocessedRequest.FinishDate:d} : ";

            try
            {
                var transactions = GetFilteredTransactions().ToList();

                decimal incomes = transactions
                    .Where(r => ConvertStringToDecimal(r.Element(Config.BetragField).Value) > 0)
                    .Sum(r => ConvertStringToDecimal(r.Element(Config.BetragField).Value));

                decimal expenses = transactions
                    .Where(r => ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0)
                    .Sum(r => ConvertStringToDecimal(r.Element(Config.BetragField).Value));

                decimal balance = incomes + expenses;

                result += $"{incomes} {expenses} = {balance}";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetSummary**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        protected List<string> GetTransactionsAccounts()
        {
            try
            {
                var accs =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                          && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                          && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                    select r.Attribute(Config.AuftragsKontoField).Value;

                return accs.Distinct().ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetTransactionsAccounts**",
                    Config.AppName + ": Unable to get Accounts", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected ObservableCollection<BoolTextCouple> GetTransactionsAccountsObsCollBoolTextCouple()
        {
            try
            {
                var accs =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                          && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                          && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r by r.Attribute(Config.AuftragsKontoField).Value into g
                    select new BoolTextCouple(!preprocessedRequest.Accounts.Contains(g.Key), g.Key);

                return new ObservableCollection<BoolTextCouple>(accs.Distinct());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetTransactionsAccounts**",
                    Config.AppName + ": Unable to get Accounts", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected ObservableCollection<BoolTextCouple> GetBuchungstextOverDateRange()
        {
            try
            {
                var res =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                          && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                          && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                    group r by r.Element(Config.BuchungsTextField).Value into g
                    select new BoolTextCouple(!preprocessedRequest.Buchungstexts.Contains(g.Key), g.Key);

                return new ObservableCollection<BoolTextCouple>(res);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetBuchungstextOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, decimal>> GetDates4RemiteeOverDateRange(DataRequest request)
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where request.SelectedRemittee.Equals(r.Element(Config.BeguenstigterField).Value)
                          && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= 0
                          && MatchesSearchFilter(r)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, decimal>(
                        r.Element(Config.WertDatumField).Value,
                        -ConvertStringToDecimal(r.Element(Config.BetragField).Value));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetDates4RemiteeOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, decimal>> GetExpensesOverCategory()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                    group r.Element(Config.BetragField).Value by r.Element(Config.CategoryField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum(n => -ConvertStringToDecimal(n)));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpensesOverCategory**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        protected List<KeyValuePair<string, decimal>> GetExpenseBeneficiary4CategoryOverDateRange(DataRequest request)
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where request.SelectedCategory.Equals(r.Element(Config.CategoryField).Value)
                          && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= 0
                          && MatchesSearchFilter(r)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, decimal>(
                        $"{r.Element(Config.WertDatumField).Value} : {r.Element(Config.BeguenstigterField).Value}",
                        -ConvertStringToDecimal(r.Element(Config.BetragField).Value));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetExpenseBeneficiary4CategoryOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private bool MatchesSearchFilter(XElement r)
        {
            if (string.IsNullOrEmpty(preprocessedRequest.ToFind))
                return true;

            return r.Element(Config.BeguenstigterField).Value.Contains(preprocessedRequest.ToFind)
                || r.Element(Config.VerwendZweckField).Value.Contains(preprocessedRequest.ToFind);
        }

        private static string TruncateString(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #endregion
    }

    delegate void UpdateUIDelegateTextBox(System.Windows.Controls.TextBox textBox);
}
