using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfApplication1.BusinessLogic;
using WpfApplication1.DAL;
using WpfApplication1.DTO;

namespace WpfApplication1
{
    public class BusinessLogicSSKA : IBusinessLogic
    {
        // When true, UpdateDataModel will not create/show the progress window.
        public bool SuppressProgressWindow { get; set; }

        private readonly CsvToXmlSSKA dataGate;
        private readonly IDataSourceProvider _dataSourceProvider;
        private readonly Dispatcher _uiDispatcher;
        private WindowProgrBar progrBar;
        private bool _isRunning;
        private readonly ResponseModel responseModel;
        private static PreprocessedDataRequest preprocessedRequest;
        private readonly TransactionQueryService _queryService;

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
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            InitializePreprocessedRequest();
            _queryService = new TransactionQueryService(_dataSourceProvider, preprocessedRequest);
            RegisterDataRequestHandlers();

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
            DiagnosticLog.Log("BusinessLogicSSKA", "UpdateDataModel called");
            DiagnosticLog.Log("BusinessLogicSSKA", $"SuppressProgressWindow: {SuppressProgressWindow}");

            PreProcessRequest(Request);

            // Only show progress window if not suppressed
            if (!SuppressProgressWindow)
            {
                // If the progress window was closed by the user or finalized earlier, recreate it.
                if (progrBar == null || !progrBar.IsLoaded)
                {
                    DiagnosticLog.Log("BusinessLogicSSKA", "Creating new progress bar window");
                    progrBar = new WindowProgrBar();
                }

                // Show the progress window if it's not currently visible. Wrap Show in try/catch
                // because calling Show on a window that has been closed can throw.
                try
                {
                    if (!progrBar.IsVisible)
                    {
                        DiagnosticLog.Log("BusinessLogicSSKA", "Showing progress bar window");
                        progrBar.Show();
                        progrBar.Activate();
                        progrBar.Topmost = true;
                        if (progrBar.pbStatus != null)
                            progrBar.pbStatus.Value = 0;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    DiagnosticLog.Log("BusinessLogicSSKA", $"InvalidOperationException showing progress bar: {ex.Message}");
                    // If the window cannot be shown because it was previously closed, recreate and show.
                    progrBar = new WindowProgrBar();
                    try
                    {
                        progrBar.Show();
                        progrBar.Activate();
                    }
                    catch (Exception ex2) { DiagnosticLog.Log("BusinessLogicSSKA", $"Failed to show recreated progress bar: {ex2.Message}"); }
                }
            }
            else
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Progress window suppressed");
            }

            if (!_isRunning)
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Starting parallel queries");
                responseModel.IsDataReady = false;
#pragma warning disable CS4014 // Fire-and-forget by design: result is applied via dispatcher callback
                RunQueriesAsync();
#pragma warning restore CS4014
            }
            else
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Queries already running, not starting new work");
            }

            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;
            DiagnosticLog.Log("BusinessLogicSSKA", "UpdateDataModel completed");
        }

        public void FilterData()
        {
            UpdateDataModel();
            responseModel.BuchungstextOverDateRange = _queryService.GetBuchungstextOverDateRange();
            responseModel.TransactionsAccountsObsCollBoolTextCouple = _queryService.GetTransactionsAccountsObsCollBoolTextCouple();
        }

        public void UpdateViewData()
        {
            if (Request.AtDate != DateTime.MinValue)
                responseModel.ExpensesAtDate = _queryService.GetExpensesAtDate(Request);

            if (!string.IsNullOrEmpty(Request.SelectedRemittee))
                responseModel.Dates4RemiteeOverDateRange = _queryService.GetDates4RemiteeOverDateRange(Request);

            if (!string.IsNullOrEmpty(Request.SelectedCategory))
                responseModel.ExpenseBeneficiary4CategoryOverDateRange = _queryService.GetExpenseBeneficiary4CategoryOverDateRange(Request);
        }

        public void FinalizeBL()
        {
            if (progrBar != null)
            {
                try
                {
                    progrBar.Close();
                }
                finally
                {
                    // Mark for recreation if later required
                    progrBar = null;
                }
            }
        }

        #region Parallel Queries

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

        private async Task RunQueriesAsync()
        {
            _isRunning = true;
            const int totalQueries = 10;
            int completedCount = 0;
            var result = new WorkerResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var tasks = new[]
                {
                    Task.Run(() => { result.ExpensesOverDateRange = _queryService.GetExpensesOverDateRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.TransactionsAccounts = _queryService.GetTransactionsAccounts();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.IncomesOverDatesRange = _queryService.GetIncomesOverDatesRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.BalanceOverDateRange = _queryService.GetBalanceOverDateRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.ExpensesInfoOverDateRange = _queryService.GetExpensesInfoOverDateRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.ExpensesOverRemiteeGroupsInDateRange = _queryService.GetExpensesOverRemiteeGroupsInDateRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.ExpensesOverRemiteeInDateRange = _queryService.GetExpensesOverRemiteeInDateRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.ExpensesOverCategory = _queryService.GetExpensesOverCategory();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.IncomesInfoOverDateRange = _queryService.GetIncomesInfoOverDateRange();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                    Task.Run(() => { result.Summary = _queryService.GetSummary();
                        ReportMonotonicProgress(Interlocked.Increment(ref completedCount), totalQueries); }),
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                DiagnosticLog.Log("BusinessLogicSSKA", $"Parallel queries error: {ex.Message}");
            }

            // Ensure the progress bar is visible for at least 500 ms so the user can see it.
            const int minDisplayMs = 500;
            int elapsed = (int)stopwatch.ElapsedMilliseconds;
            if (elapsed < minDisplayMs)
                await Task.Delay(minDisplayMs - elapsed);

            await _uiDispatcher.InvokeAsync(() => ApplyResult(result), DispatcherPriority.Normal);
        }

        private void ReportMonotonicProgress(int completed, int total)
        {
            int percent = completed * 100 / total;
            if (progrBar != null && progrBar.pbStatus != null)
            {
                try
                {
                    progrBar.pbStatus.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() => progrBar.pbStatus.Value = percent));
                }
                catch (InvalidOperationException)
                {
                    // Window may have been closed concurrently — ignore.
                }
            }
        }

        private void ApplyResult(WorkerResult result)
        {
            DiagnosticLog.Log("BusinessLogicSSKA", "ApplyResult called");

            try
            {
                if (progrBar != null && progrBar.IsVisible)
                    progrBar.Hide();
            }
            catch (Exception ex)
            {
                DiagnosticLog.Log("BusinessLogicSSKA", $"Error hiding progress bar: {ex.Message}");
            }

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
            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;
            _isRunning = false;
            DiagnosticLog.Log("BusinessLogicSSKA", "Parallel queries completed, data is ready");
        }

        #endregion

        #region Request Processing

        private void InitializePreprocessedRequest()
        {
            preprocessedRequest = new PreprocessedDataRequest
            {
                Accounts = new List<string>(),
                Buchungstexts = new List<string>(),
                IncomesLowestValue = decimal.Zero,
                IncomesHighestValue = decimal.MaxValue,
                ExpensesLowestValue = decimal.Zero,
                ExpensesHighestValue = decimal.MaxValue,
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
                if (!decimal.TryParse(request.Filters.ExpensesLessThan, out decimal expLessThan))
                    expLessThan = decimal.MaxValue;

                decimal.TryParse(request.Filters.ExpensesMoreThan, out decimal expMoreThan);

                if (!decimal.TryParse(request.Filters.IncomesLessThan, out decimal incomsHighestValue))
                    incomsHighestValue = decimal.MaxValue;

                decimal.TryParse(request.Filters.IncomesMoreThan, out decimal incomsLowestValue);

                preprocessedRequest.Buchungstexts.Clear();
                preprocessedRequest.Buchungstexts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.BuchungstextValues));

                preprocessedRequest.Accounts.Clear();
                preprocessedRequest.Accounts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.UserAccounts));

                preprocessedRequest.ToFind = request.Filters.ToFind;
                preprocessedRequest.ExpensesLowestValue = expMoreThan;
                preprocessedRequest.ExpensesHighestValue = expLessThan;
                preprocessedRequest.IncomesLowestValue = incomsLowestValue;
                preprocessedRequest.IncomesHighestValue = incomsHighestValue;
            }
        }

        private static List<string> ConvertObsCollBoolTextCoupleToList(ObservableCollection<BoolTextCouple> values)
        {
            if (values == null)
                return new List<string>();

            return values.Where(elem => !elem.IsSelected).Select(elem => elem.Text).ToList();
        }

        #endregion

        public List<KeyValuePair<string, decimal>> GetExpensesOverCategoryForDateRange(DateTime begin, DateTime end)
        {
            var tempRequest = new WpfApplication1.DTO.PreprocessedDataRequest
            {
                BeginDate = begin,
                FinishDate = end,
                Accounts = preprocessedRequest.Accounts,
                Buchungstexts = preprocessedRequest.Buchungstexts,
                ExpensesLowestValue = preprocessedRequest.ExpensesLowestValue,
                ExpensesHighestValue = preprocessedRequest.ExpensesHighestValue,
                IncomesLowestValue = preprocessedRequest.IncomesLowestValue,
                IncomesHighestValue = preprocessedRequest.IncomesHighestValue,
                ToFind = preprocessedRequest.ToFind
            };
            var tempService = new TransactionQueryService(_dataSourceProvider, tempRequest);
            return tempService.GetExpensesOverCategory();
        }
    }

    delegate void UpdateUIDelegateTextBox(System.Windows.Controls.TextBox textBox);
}
