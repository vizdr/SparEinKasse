using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
        private WindowProgrBar progrBar;
        private readonly BackgroundWorker worker;
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
            InitializePreprocessedRequest();
            _queryService = new TransactionQueryService(_dataSourceProvider, preprocessedRequest);
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

            if (!worker.IsBusy)
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Starting background worker");
                responseModel.IsDataReady = false;
                worker.RunWorkerAsync();
            }
            else
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Worker is busy, not starting new work");
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
                result.ExpensesOverDateRange = _queryService.GetExpensesOverDateRange();
                bgWorker.ReportProgress(10);

                result.TransactionsAccounts = _queryService.GetTransactionsAccounts();
                bgWorker.ReportProgress(20);

                result.IncomesOverDatesRange = _queryService.GetIncomesOverDatesRange();
                bgWorker.ReportProgress(30);

                result.BalanceOverDateRange = _queryService.GetBalanceOverDateRange();
                bgWorker.ReportProgress(40);

                result.ExpensesInfoOverDateRange = _queryService.GetExpensesInfoOverDateRange();
                bgWorker.ReportProgress(50);

                result.ExpensesOverRemiteeGroupsInDateRange = _queryService.GetExpensesOverRemiteeGroupsInDateRange();
                bgWorker.ReportProgress(60);

                result.ExpensesOverRemiteeInDateRange = _queryService.GetExpensesOverRemiteeInDateRange();
                bgWorker.ReportProgress(70);

                result.ExpensesOverCategory = _queryService.GetExpensesOverCategory();
                bgWorker.ReportProgress(80);

                result.IncomesInfoOverDateRange = _queryService.GetIncomesInfoOverDateRange();
                bgWorker.ReportProgress(90);

                result.Summary = _queryService.GetSummary();
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
            // If progress window is available update it on its dispatcher. Otherwise skip.
            if (progrBar != null && progrBar.pbStatus != null)
            {
                try
                {
                    progrBar.pbStatus.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() => progrBar.pbStatus.Value = e.ProgressPercentage));
                }
                catch (InvalidOperationException)
                {
                    // Window may have been closed concurrently � ignore progress update.
                }
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DiagnosticLog.Log("BusinessLogicSSKA", "Worker_RunWorkerCompleted called");

            if (e.Error != null)
            {
                DiagnosticLog.Log("BusinessLogicSSKA", $"Worker completed with ERROR: {e.Error.Message}");
                DiagnosticLog.Log("BusinessLogicSSKA", $"Stack trace: {e.Error.StackTrace}");
            }

            // Use Application.Current.Dispatcher as fallback if progrBar was closed during work
            var dispatcher = progrBar?.Dispatcher ?? Application.Current?.Dispatcher;
            DiagnosticLog.Log("BusinessLogicSSKA", $"Dispatcher available: {dispatcher != null}, progrBar: {progrBar != null}");

            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Invoking ApplyWorkerResult via dispatcher");
                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => ApplyWorkerResult(e)));
            }
            else
            {
                DiagnosticLog.Log("BusinessLogicSSKA", "Calling ApplyWorkerResult directly");
                ApplyWorkerResult(e);
            }
        }

        private void ApplyWorkerResult(RunWorkerCompletedEventArgs e)
        {
            DiagnosticLog.Log("BusinessLogicSSKA", "ApplyWorkerResult called");

            // progrBar may have been closed if the window was finalized during data loading,
            // or may never have been created if SuppressProgressWindow was true
            try
            {
                if (progrBar != null && progrBar.IsVisible)
                {
                    DiagnosticLog.Log("BusinessLogicSSKA", "Hiding progress bar");
                    progrBar.Hide();
                }
                else
                {
                    DiagnosticLog.Log("BusinessLogicSSKA", $"Progress bar not hidden: progrBar={progrBar != null}, IsVisible={progrBar?.IsVisible}");
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.Log("BusinessLogicSSKA", $"Error hiding progress bar: {ex.Message}");
            }

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
                DiagnosticLog.Log("BusinessLogicSSKA", "Worker result applied successfully, data is ready");
            }
            else
            {
                DiagnosticLog.Log("BusinessLogicSSKA", $"Worker result type: {e.Result?.GetType().Name ?? "null"}");
            }

            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;
            DiagnosticLog.Log("BusinessLogicSSKA", "ApplyWorkerResult completed");
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
    }

    delegate void UpdateUIDelegateTextBox(System.Windows.Controls.TextBox textBox);
}
