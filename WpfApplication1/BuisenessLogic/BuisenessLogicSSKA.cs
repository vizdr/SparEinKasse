using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.Globalization;
using System.Collections.ObjectModel;
using WpfApplication1.DAL;
using WpfApplication1.DTO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Threading;
using WpfApplication1.BuisenessLogic;

namespace WpfApplication1
{
    public class BuisenessLogicSSKA : IBuisenessLogic
    {
        private readonly CsvToXmlSSKA dataGate;
        public Action<DataRequest> updateChart = delegate { }; // preinitialization with empty invokation list
        private readonly WindowProgrBar progrBar;
        private static PreprocessedDataRequest preprocessedRequest;
        BackgroundWorker worker;
        private ResponseModel responseModel = ResponseModel.GetInstance();
        public ResponseModel ResponseModel
        {
            get => responseModel;
        }
        private static AccountsLogic al;
        private static readonly BuisenessLogicSSKA instance = new BuisenessLogicSSKA(al);

        public static BuisenessLogicSSKA GetInstance()
        {
            return instance;
        }
        private BuisenessLogicSSKA(AccountsLogic accountsLogic)
        {
            al = accountsLogic;
            progrBar = new WindowProgrBar();
            dataGate = new CsvToXmlSSKA(al);

            Request = DataRequest.GetInstance();
            InitializeHandledRequest();
            RegisterDataRequestHandlers();
            
            RegisterMethodsForProgressBar();
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
            // Initial charts
            UpdateDataModel();
        }

        private void RegisterDataRequestHandlers( )
        {
            Request.DataRequested += delegate { UpdateDataModel(); };
            Request.FilterValuesRequested += delegate { FilterData(); };
            Request.DataBankUpdateRequested += delegate { UpdateData(); };
            Request.ViewDataRequested += delegate { UpdateViewData(); };
        }

        private void RegisterMethodsForProgressBar()
        {
            updateChart += delegate { responseModel.ExpensesOverDateRange = GetExpensesOverDateRange(); };
            updateChart += delegate { responseModel.TransactionsAccounts = GetTransactionsAccounts(); };
            updateChart += delegate { responseModel.IncomesOverDatesRange = GetIncomesOverDatesRange(); };
            updateChart += delegate { responseModel.BalanceOverDateRange = GetBalanceOverDateRange(); };           
            updateChart += delegate { responseModel.ExpensesInfoOverDateRange = GetExpensesInfoOverDateRange(); };
            updateChart += delegate { responseModel.ExpensesOverRemiteeGroupsInDateRange = GetExpensesOverRemiteeGroupsInDateRange(); };
            updateChart += delegate { responseModel.ExpensesOverRemiteeInDateRange = GetExpensesOverRemiteeInDateRange(); };
            updateChart += delegate { responseModel.ExpensesOverCategory = GetExpensesOverCategory(); };           
            updateChart += delegate { responseModel.IncomesInfoOverDateRange = GetIncomesInfoOverDateRange(); };
            updateChart += delegate { responseModel.Summary = GetSummary(); };
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int qty2Invoke = updateChart.GetInvocationList().Length;
            for (int ctr = 0; ctr < qty2Invoke; ctr++)
            {
                // (ctr + 1) would be adjusted to 9 calls of functions 
                int progr = (ctr + 1) * 10;

                var updChart = updateChart.GetInvocationList()[ctr];
                updChart.DynamicInvoke(Request);
                Thread.Sleep(60);

                (sender as BackgroundWorker).ReportProgress(progr);

                Thread.Sleep(60);
            }
            e.Result = 0;
            Thread.Sleep(10);           
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Checking if this thread has access to the object.
        if (progrBar.pbStatus.Dispatcher.CheckAccess())
            {
                // This thread has access so it can update the UI thread.
                progrBar.pbStatus.Value = e.ProgressPercentage;
            }
            else
            {
                // This thread does not have access to the UI thread.
                // Place the update method on the Dispatcher of the UI thread.
                progrBar.pbStatus.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new UpdateUIDelegateProgrBar(delegate { progrBar.pbStatus.Value = e.ProgressPercentage; }), progrBar.pbStatus);
            }
 
            if (e.UserState != null)
                MessageBox.Show("UserState is " + e.UserState.ToString());
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (progrBar.IsVisible)
            {
                if (progrBar.pbStatus.Dispatcher.CheckAccess())
                {
                    // This thread has access so it can update the UI thread.
                    progrBar.Hide();
                }
                else
                {
                    // This thread does not have access to the UI thread.
                    // Place the update method on the Dispatcher of the UI thread.
                    progrBar.pbStatus.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new UpdateUIDelegateProgrBar(delegate { progrBar.Hide(); }), progrBar.pbStatus);
                }
                try 
                {
                    if (Thread.CurrentThread.IsBackground)
                        Thread.CurrentThread.Abort();
                }
                catch (ThreadAbortException ex)
                {
                    Console.WriteLine("Background thread is intentionaly aborted, UI thread becomes owner");
                }               
            }
            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;
        }

        private decimal ConvertStringToDecimal(string src)
        {
            decimal res = 0m;
            try
            {
                res = decimal.Parse(src, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                MessageBox.Show(e.Message, "Unable to parse string in the currency. Try to change the currency separator symbol in Windows Regional settings and reload the data.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return res;
        }
        private static void PreProcessRequest(DataRequest request)
        {
            if (request == null)
                return;

            preprocessedRequest.AtDate = request.AtDate;
            preprocessedRequest.SelectedRemittee = request.SelectedRemittee;

            preprocessedRequest.BeginDate = request.TimeSpan.Item1;
            preprocessedRequest.FinishDate = request.TimeSpan.Item2 < request.TimeSpan.Item1 ? request.TimeSpan.Item1 : request.TimeSpan.Item2;

            if(request.Filters.UserAccounts.Count > 0)
            {
                if (!decimal.TryParse(request.Filters.ExpenciesLessThan, out decimal expLessThan))
                {
                    expLessThan = decimal.MaxValue;
                }

                decimal.TryParse(request.Filters.ExpenciesMoreThan, out decimal expMoreThan);
                if (!decimal.TryParse(request.Filters.IncomesLessThan, out decimal incomsHighestValue))
                {
                    incomsHighestValue = decimal.MaxValue;
                }

                decimal.TryParse(request.Filters.IncomesMoreThan, out decimal incomsLowestValue);
                if(preprocessedRequest.Buchungstexts.Count > 0)
                {
                    preprocessedRequest.Buchungstexts.Clear();
                }
                preprocessedRequest.Buchungstexts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.BuchungstextValues));
                if (preprocessedRequest.Accounts.Count > 0)
                {
                    preprocessedRequest.Accounts.Clear();
                }
                preprocessedRequest.Accounts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.UserAccounts));
                preprocessedRequest.ToFind = request.Filters.ToFind;

                preprocessedRequest.ExpencesLowestValue = expMoreThan;
                preprocessedRequest.ExpencesHighestValue = expLessThan;
                preprocessedRequest.IncomsLowestValue = incomsLowestValue;
                preprocessedRequest.IncomsHighestValue = incomsHighestValue;
            }
        }
       

        #region IBuisenessLogic Members
        //public ResponseModel ResponseModel { get; }
        public DataRequest Request { get; }
        public void UpdateData()
        {
            if (dataGate.UpdateDataBank())
            {
                UpdateDataModel();
            }
        }
        public void UpdateDataModel()
        {
            PreProcessRequest(Request);

            if (!progrBar.IsVisible)
            {
                progrBar.Show();
                progrBar.pbStatus.Value = 0;
            }
            // IProgress<int> progress = new Progress<int>(s => progrBar.pbStatus.Value = s);
            //  progress.Report( int value )
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                Thread.Sleep(50);
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
            if (Request.SelectedRemittee != null)
                responseModel.Dates4RemiteeOverDateRange = Request.SelectedRemittee == string.Empty? responseModel.Dates4RemiteeOverDateRange : GetDates4RemiteeOverDateRange(Request);
            if (Request.SelectedCategory != null)
                responseModel.ExpenceBeneficiary4CategoryOverDateRange = Request.SelectedCategory == string.Empty ? responseModel.ExpenceBeneficiary4CategoryOverDateRange : GetExpenseBeneficiary4CategoryOverDateRange(Request);
        }
        #endregion

        protected List<KeyValuePair<string, decimal>> GetExpensesOverDateRange()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key/*.Substring(5)*/, g.Sum(n => -ConvertStringToDecimal(n)));

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesOverDateRange**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeInDateRange()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < decimal.Zero
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<string>(n => -ConvertStringToDecimal(n))
                    );

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpencesOverRemiteeInDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, string>> GetExpensesInfoOverDateRange()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= decimal.Zero
                                        &&
                                        (
                                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : preprocessedRequest.ToFind)
                                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : preprocessedRequest.ToFind)
                                        )
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, string>(r.Element(Config.WertDatumField).Value,
                        " ** " + r.Element(Config.BeguenstigterField).Value +
                        " ** " + r.Element(Config.VerwendZweckField).Value.Substring(0, r.Element(Config.VerwendZweckField).Value.Length < 120 ? r.Element(Config.VerwendZweckField).Value.Length : 120 ) +
                        " ** " + ConvertStringToDecimal(r.Element(Config.BetragField).Value)

                   );
                return res.ToList<KeyValuePair<string, string>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesInfoOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;

        }
        protected List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeGroupsInDateRange()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField) // .Elements
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= decimal.Zero
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BuchungsTextField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<string>( n => -ConvertStringToDecimal(n)));

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpencesOverRemiteeInDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, decimal>> GetExpensesAtDate(DataRequest request)
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) == request.AtDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < decimal.Zero
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g

                    select new KeyValuePair<string, decimal>(g.Key/*.Substring(5)*/, g.Sum<string>(n => -ConvertStringToDecimal(n) ));

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesAtDate**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, string>> GetIncomesInfoOverDateRange()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= preprocessedRequest.IncomsLowestValue
                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= preprocessedRequest.IncomsHighestValue
                        &&
                        (
                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : preprocessedRequest.ToFind)
                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : preprocessedRequest.ToFind)
                        )
                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)

                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, string>(r.Element(Config.WertDatumField).Value,
                        " ** " + r.Element(Config.BeguenstigterField).Value +
                        " ** " + r.Element(Config.VerwendZweckField).Value.Substring(0, r.Element(Config.VerwendZweckField).Value.Length < 120 ? r.Element(Config.VerwendZweckField).Value.Length : 120) +
                        " ** " + ConvertStringToDecimal(r.Element(Config.BetragField).Value)
                   );
                return res.ToList<KeyValuePair<string, string>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetIncomesInfoOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;

        }
        protected ObservableCollection<KeyValuePair<string, decimal>> GetIncomesOverDatesRange()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= decimal.Zero
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key.Substring(5), g.Sum<string>(
                           n => ConvertStringToDecimal(n)));
                   
                res = res.Where(paar => (paar.Value >= preprocessedRequest.IncomsLowestValue) && (paar.Value <= preprocessedRequest.IncomsHighestValue));
                return new ObservableCollection<KeyValuePair<string, decimal>>(res.ToList<KeyValuePair<string, decimal>>());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<DateTime, decimal>> GetBalanceOverDateRange()
        {
            List<KeyValuePair<DateTime, decimal>> resultedList = new List<KeyValuePair<DateTime, decimal>>();
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value) // ??
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<DateTime, decimal>(DateTime.Parse(g.Key).Date, g.Sum<string>(
                           n => ConvertStringToDecimal(n)
                           )
                   );
                List<KeyValuePair<DateTime, decimal>> inputList = res.ToList<KeyValuePair<DateTime, decimal>>();

                IEnumerator<KeyValuePair<DateTime, decimal>> inputEnumerator = inputList.GetEnumerator();
                decimal akku = 0m;

                if (inputEnumerator.MoveNext())
                    for (DateTime currDate = preprocessedRequest.BeginDate/*.Date.AddDays(1)*/; !preprocessedRequest.FinishDate.Date.Equals(currDate.Date.AddDays(-1)); currDate = currDate.Date.AddDays(1))
                    {
                        if (inputEnumerator.Current.Key.Equals(currDate))
                        {
                            akku += inputEnumerator.Current.Value;
                            resultedList.Add(new KeyValuePair<DateTime, decimal>(inputEnumerator.Current.Key, akku));
                            inputEnumerator.MoveNext();
                        }
                        else resultedList.Add(new KeyValuePair<DateTime, decimal>(currDate, akku));
                    }
                return resultedList;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetBalanceOverDateRange**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        protected string GetSummary()
        {
            string result = String.Format("Total: {0:d} - {1:d} : ", preprocessedRequest.BeginDate, preprocessedRequest.FinishDate);

            try
            {
                var resIncomes =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) > 0
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Elements(Config.BetragField).Single() by r.Parent.Element(Config.TransactionField).Name.LocalName into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<XElement>(
                                                               n => ConvertStringToDecimal(n.Value)
                                                               ));
                var resExpences =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Elements(Config.BetragField).Single() by r.Parent.Element(Config.TransactionField).Name.LocalName into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<XElement>(
                                                               n => ConvertStringToDecimal(n.Value)
                                                               ));
                var resBalances =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Elements(Config.BetragField).Single() by r.Parent.Element(Config.TransactionField).Name.LocalName into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<XElement>(
                                                               n => ConvertStringToDecimal(n.Value)
                                                               ));
                result += resIncomes.FirstOrDefault<KeyValuePair<string, decimal>>().Value +
                    " " + resExpences.FirstOrDefault<KeyValuePair<string, decimal>>().Value +
                    " = " + resBalances.FirstOrDefault<KeyValuePair<string, decimal>>().Value;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetSummary**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        protected List<string> GetTransactionsAccounts()
        {
            try
            {
                var accs =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                    select r.Attribute(Config.AuftragsKontoField).Value;
                return accs.Distinct<string>().ToList<string>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetTransactionsAccounts**",
                    Config.AppName + ": Unable to get Accounts", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        } 
        
        protected ObservableCollection<BoolTextCouple> GetTransactionsAccountsObsCollBoolTextCouple()
        {
            if (preprocessedRequest.Accounts.Count > 0)
            {
                var accs =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                            && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                        group r.Attribute(Config.AuftragsKontoField).Value by r.Attribute(Config.AuftragsKontoField).Value into g
                        select new BoolTextCouple(!preprocessedRequest.Accounts.Contains(g.Key), g.Key);
                return new ObservableCollection<BoolTextCouple>(accs.Distinct<BoolTextCouple>());
            }
            else
            {
                try
                {
                    var accs =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                            && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                        group r.Attribute(Config.AuftragsKontoField).Value by r.Attribute(Config.AuftragsKontoField).Value into g
                        select new BoolTextCouple(true, g.Key);
                    return new ObservableCollection<BoolTextCouple>(accs.Distinct<BoolTextCouple>());
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetTransactionsAccounts**",
                        Config.AppName + ": Unable to get Accounts", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return null;
        }
        protected ObservableCollection<BoolTextCouple> GetBuchungstextOverDateRange()
        {
            if (preprocessedRequest.Buchungstexts.Count > 0)
            {
                try
                {
                    var res =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                            && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                        group r.Element(Config.WertDatumField).Value by r.Element(Config.BuchungsTextField).Value into g

                        select new BoolTextCouple(!preprocessedRequest.Buchungstexts.Contains(g.Key), g.Key);

                    return new ObservableCollection<BoolTextCouple>(res);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetBuchungstextOverDateRange**",
                        Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    var res =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                            && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                        group r.Element(Config.WertDatumField).Value by r.Element(Config.BuchungsTextField).Value into g

                        select new BoolTextCouple(true, g.Key);

                    return new ObservableCollection<BoolTextCouple>(res);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetBuchungstextOverDateRange**",
                        Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return null;
        }
        protected static List<string> ConvertObsCollBoolTextCoupleToList(ObservableCollection<BoolTextCouple> values)
        {
            List<string> result = new List<string>();
            if (values != null)
            {
                foreach (BoolTextCouple elem in values)
                    if (!elem.IsSelected)
                        result.Add(elem.Text);
            }
            return result;
        }

              
        protected List<KeyValuePair<string, decimal>> GetDates4RemiteeOverDateRange(DataRequest request)
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && request.SelectedRemittee.Equals(r.Element(Config.BeguenstigterField).Value)
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= decimal.Zero
                                        &&
                                        (
                                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : preprocessedRequest.ToFind)
                                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : preprocessedRequest.ToFind)
                                        )
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)

                    select new KeyValuePair<string, decimal>(r.Element(Config.WertDatumField).Value,
                          (-ConvertStringToDecimal(r.Element(Config.BetragField).Value)));

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetDates4RemiteeOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        protected List<KeyValuePair<string, decimal>> GetExpensesOverCategory()
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.CategoryField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key/*.Substring(5)*/, g.Sum(n => -ConvertStringToDecimal(n)));

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesOverDateRange**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, decimal>> GetExpenseBeneficiary4CategoryOverDateRange(DataRequest request)
        {
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= preprocessedRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= preprocessedRequest.FinishDate
                                        && request.SelectedCategory.Equals(r.Element(Config.CategoryField).Value)
                                        && !preprocessedRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= decimal.Zero
                                        &&
                                        (
                                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : preprocessedRequest.ToFind)
                                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(preprocessedRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : preprocessedRequest.ToFind)
                                        )
                                        && !preprocessedRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)

                    select new KeyValuePair<string, decimal>(r.Element(Config.WertDatumField).Value + " : " + r.Element(Config.BeguenstigterField).Value,
                          (-ConvertStringToDecimal(r.Element(Config.BetragField).Value)));

                res = ApplyRangeFilter(res);
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetDates4RemiteeOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
            private int GetPercentage(int numerator, int denominator)
        {
            int res;
            if (numerator++ >= denominator)
            {
                res = 0;
            }
            else
            {
                res = (int)(((float)numerator / (float)denominator) * 100);
            }

            return res;
        }
        public void FinalizeBL()
        {
            if (progrBar != null)
                progrBar.Close();
        }
        private void InitializeHandledRequest()
        {
            preprocessedRequest = new PreprocessedDataRequest();

            preprocessedRequest.Accounts = new List<string>(); 
            preprocessedRequest.Buchungstexts = new List<string>();
            preprocessedRequest.IncomsLowestValue = Decimal.Zero;
            preprocessedRequest.IncomsHighestValue = Decimal.MaxValue;
            preprocessedRequest.ExpencesLowestValue = Decimal.Zero;
            preprocessedRequest.ExpencesHighestValue = Decimal.MaxValue;
        }
        private IEnumerable<KeyValuePair<string, decimal>> ApplyRangeFilter(IEnumerable<KeyValuePair<string, decimal>> res)
        {
            return res.Where(paar => (paar.Value >= preprocessedRequest.ExpencesLowestValue) && (paar.Value <= preprocessedRequest.ExpencesHighestValue));
        }
    }

    delegate void UpdateUIDelegateProgrBar( System.Windows.Controls.ProgressBar progressBar);
    delegate void UpdateUIDelegateTextBox(System.Windows.Controls.TextBox textBox);
}
