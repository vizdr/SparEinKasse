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


namespace WpfApplication1
{
    public class BuisenessLogicSSKA : IBuisenessLogic
    {
        private CsvToXmlSSKA dataGate;
        private ChartsModel chartsModel;
        private DataRequest request;
        public Action<DataRequest> updateChart;
        private WindowProgrBar progrBar;
        static Mutex mutexObj;

        private static BuisenessLogicSSKA instance = new BuisenessLogicSSKA();

        public static BuisenessLogicSSKA GetInstance()
        {
            return instance;
        }
        private BuisenessLogicSSKA()
        {

            progrBar = new WindowProgrBar();

            dataGate = new CsvToXmlSSKA();
            chartsModel = ChartsModel.GetInstance();
            request = DataRequest.GetInstance();

            request.DataRequested += delegate { this.UpdateDataModel(); };
            request.FilterValuesRequested += delegate { this.FilterData(); };
            request.DataBankUpdateRequested += delegate { UpdateData(); };
            request.ViewDataRequested += delegate { this.UpdateViewData(); };

            updateChart += delegate { /* intended empty to delay */ };
            updateChart += delegate { chartsModel.TransactionsAccounts = GetTransactionsAccounts(request); };
            updateChart += delegate { chartsModel.IncomesOverDatesRange = GetIncomesOverDatesRange(request); };
            updateChart += delegate { chartsModel.BalanceOverDateRange = GetBalanceOverDateRange(request); };
            updateChart += delegate { chartsModel.ExpensesOverDateRange = GetExpensesOverDateRange(request); };
            updateChart += delegate { chartsModel.ExpensesInfoOverDateRange = GetExpensesInfoOverDateRange(request); };
            updateChart += delegate { chartsModel.ExpensesOverRemiteeGroupsInDateRange = GetExpensesOverRemiteeGroupsInDateRange(request); };
            updateChart += delegate { chartsModel.ExpensesOverRemiteeInDateRange = GetExpensesOverRemiteeInDateRange(request); };
            updateChart += delegate { chartsModel.Summary = GetSummary(request); };           
            updateChart += delegate { chartsModel.IncomesInfoOverDateRange = GetIncomesInfoOverDateRange(request); };

            UpdateDataModel();

        }

        static BuisenessLogicSSKA()
        {
            mutexObj = new Mutex();
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
        private PreprocessedDataRequest HandleRequest(DataRequest request)
        {
            PreprocessedDataRequest result = new PreprocessedDataRequest();

            List<string> buchungstexts = new List<string>();
            List<string> accounts = new List<string>();
            Decimal incomsLowestValue = Decimal.Zero;
            Decimal incomsHighestValue = Decimal.MaxValue;
            Decimal expLowestValue = Decimal.MaxValue;
            Decimal expHighestValue = Decimal.Zero;
            result.AtDate = request.AtDate;
            result.SelectedRemittee = request.SelectedRemittee;

            result.BeginDate = request.BeginDate;
            if (request.EndDate < request.BeginDate)
                result.FinishDate = request.BeginDate;
            else result.FinishDate = request.EndDate;

            if (request.Filters != null)
            {
                if (!decimal.TryParse(request.Filters.ExpenciesLessThan, out expLowestValue))
                    expLowestValue = Decimal.MaxValue;
                decimal.TryParse(request.Filters.ExpenciesMoreThan, out expHighestValue);
                if (!decimal.TryParse(request.Filters.IncomesLessThan, out incomsHighestValue))
                    incomsHighestValue = Decimal.MaxValue;
                decimal.TryParse(request.Filters.IncomesMoreThan, out incomsLowestValue);
                buchungstexts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.BuchungstextValues));
                accounts.AddRange(ConvertObsCollBoolTextCoupleToList(request.Filters.Accounts));
                result.ToFind = request.Filters.ToFind;
            }
            result.ExpencesLowestValue = expLowestValue;
            result.ExpencesHighestValue = expHighestValue;
            result.IncomsLowestValue = incomsLowestValue;
            result.IncomsHighestValue = incomsHighestValue;
            result.Buchungstexts = buchungstexts;
            result.Accounts = accounts;
            return result;
        }

        #region IBuisenessLogic Members
        public ChartsModel ChartsModel
        {
            get { return chartsModel; }
        }
        public DataRequest Request
        {
            get { return request; }
        }
        public void UpdateData()
        {
            if (dataGate.UpdateDataBank())
                UpdateDataModel();
        }
        public async void UpdateDataModel()
        {
            if (!progrBar.IsVisible)
            {
                progrBar.Show();
                progrBar.pbStatus.Value = 0;
            }
            IProgress<int> progress = new Progress<int>(s => progrBar.pbStatus.Value = s);

            int qty2Invoke = updateChart.GetInvocationList().Length;
            for (int ctr = 0; ctr < qty2Invoke; ctr++)
            {
                // adjusted to 9 calls of functions 
                int progr = (ctr + 1) * 10;

                var updChart = updateChart.GetInvocationList()[ctr];
                updChart.DynamicInvoke(request);
                await Task.Factory.StartNew<int>(
                                             () =>
                                             {
                                                 progress.Report(progr);
                                                 Thread.Sleep(35);
                                                 return 0;
                                             },
                                             TaskCreationOptions.LongRunning);
            }

            if (progrBar.IsVisible)
            {
                progrBar.Hide();
            }
        }
        public void FilterData()
        {
            UpdateDataModel();
            chartsModel.BuchungstextOverDateRange = GetBuchungstextOverDateRange(request);
            chartsModel.TransactionsAccountsObsCollBoolTextCouple = GetTransactionsAccountsObsCollBoolTextCouple(request);
        }
        public void UpdateViewData()
        {
            if (request.AtDate != null)
                chartsModel.ExpensesAtDate = GetExpensesAtDate(request);
            if (request.SelectedRemittee != null)
                chartsModel.Dates4RemiteeOverDateRange = GetDates4RemiteeOverDateRange(request);
        }
        #endregion

        protected List<KeyValuePair<string, decimal>> GetExpensesOverDateRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);

            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key/*.Substring(5)*/, g.TakeWhile<string>(
                           m => ConvertStringToDecimal(m) <= -handledRequest.ExpencesHighestValue && ConvertStringToDecimal(m) >= -handledRequest.ExpencesLowestValue).Sum<string>(
                           n => -ConvertStringToDecimal(n)
                           )
                   );
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesOverDateRange**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeInDateRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);

            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        //&& ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= -handledRequest.ExpencesHighestValue
                                        //&& ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= -handledRequest.ExpencesLowestValue
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.TakeWhile<string>
                        (
                           m => ConvertStringToDecimal(m) <= -handledRequest.ExpencesHighestValue
                               && ConvertStringToDecimal(m) >= -handledRequest.ExpencesLowestValue
                         ).Sum<string>(n => -ConvertStringToDecimal(n))
                    );
                //    select new
                //    {
                //        Betrag = g.Key,
                //        Gr = g.TakeWhile<string>
                //            (
                //               m => ConvertStringToDecimal(m) <= -handledRequest.ExpencesHighestValue
                //                   && ConvertStringToDecimal(m) >= -handledRequest.ExpencesLowestValue
                //             ).Sum<string>(n => -ConvertStringToDecimal(n)),
                //        Values = from p in g select p

                //    };
                //List<KeyValuePair<string, decimal>> temp = new List<KeyValuePair<string, decimal>>();
                //foreach (var group in res)
                //{
                //    //MessageBox.Show(group.Betrag);                   
                //    temp.Add(new KeyValuePair<string, decimal>(group.Betrag, group.Gr));
                //    //foreach (string t in group.Values)
                //    //    MessageBox.Show(t);                   
                //}
                // return temp;
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpencesOverRemiteeInDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
        protected List<KeyValuePair<string, string>> GetExpensesInfoOverDateRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= -handledRequest.ExpencesHighestValue
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= -handledRequest.ExpencesLowestValue
                                        &&
                                        (
                                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(handledRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : handledRequest.ToFind)
                                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(handledRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : handledRequest.ToFind)
                                        )
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, string>(r.Element(Config.WertDatumField).Value,
                        " ** " + r.Element(Config.BeguenstigterField).Value +
                        " ** " + r.Element(Config.VerwendZweckField).Value +
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
        protected List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeGroupsInDateRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);

            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField) // .Elements
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= -handledRequest.ExpencesHighestValue
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= -handledRequest.ExpencesLowestValue
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BuchungsTextField).Value into g
                    orderby g.Key
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<string>(
                           n => -ConvertStringToDecimal(n)
                           )
                   );
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
            PreprocessedDataRequest handledRequest = HandleRequest(request);

            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) == handledRequest.AtDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g

                    select new KeyValuePair<string, decimal>(g.Key/*.Substring(5)*/, g.TakeWhile<string>(
                           m => ConvertStringToDecimal(m) <= -handledRequest.ExpencesHighestValue && ConvertStringToDecimal(m) >= -handledRequest.ExpencesLowestValue).Sum<string>(
                           n => -ConvertStringToDecimal(n)
                           )
                   );
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesAtDate**",
                    Config.AppName + ": Unable to get Data.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        protected List<KeyValuePair<string, string>> GetIncomesInfoOverDateRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= handledRequest.IncomsLowestValue
                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= handledRequest.IncomsHighestValue
                        &&
                        (
                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(handledRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : handledRequest.ToFind)
                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(handledRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : handledRequest.ToFind)
                        )
                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)

                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)
                    select new KeyValuePair<string, string>(r.Element(Config.WertDatumField).Value,
                        " ** " + r.Element(Config.BeguenstigterField).Value +
                        " ** " + r.Element(Config.VerwendZweckField).Value +
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
        protected List<KeyValuePair<string, decimal>> GetIncomesOverDatesRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= handledRequest.IncomsLowestValue
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= handledRequest.IncomsHighestValue
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key.Substring(5), g.Sum<string>(
                           n => ConvertStringToDecimal(n)
                           // g.TakeWhile<XElement>(
                           //m => ConvertStringToDecimal(m.Value) <= incomsHighestValue && ConvertStringToDecimal(m.Value) >= incomsLowestValue).Sum<XElement>(
                           //n => ConvertStringToDecimal(n.Value)
                           )
                   );
                //chartsModel.IncomesOverDatesRange = res.ToList<KeyValuePair<string, decimal>>();
                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetExpensesOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        protected List<KeyValuePair<DateTime, decimal>> GetBalanceOverDateRange(DataRequest request)
        {
            List<KeyValuePair<DateTime, decimal>> resultedList = new List<KeyValuePair<DateTime, decimal>>();
            PreprocessedDataRequest handledRequest = HandleRequest(request);

            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value) // ??
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
                    for (DateTime currDate = handledRequest.BeginDate/*.Date.AddDays(1)*/; !handledRequest.FinishDate.Date.Equals(currDate.Date.AddDays(-1)); currDate = currDate.Date.AddDays(1))
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

        protected string GetSummary(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            string result = String.Format("Total: {0:d} - {1:d} : ", handledRequest.BeginDate, handledRequest.FinishDate);

            try
            {
                var resIncomes =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) > 0
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Elements(Config.BetragField).Single() by r.Parent.Element(Config.TransactionField).Name.LocalName into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<XElement>(
                                                               n => ConvertStringToDecimal(n.Value)
                                                               ));
                var resExpences =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Elements(Config.BetragField).Single() by r.Parent.Element(Config.TransactionField).Name.LocalName into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum<XElement>(
                                                               n => ConvertStringToDecimal(n.Value)
                                                               ));
                var resBalances =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
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

        protected List<string> GetTransactionsAccounts(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            try
            {
                var accs =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
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
        protected ObservableCollection<BoolTextCouple> GetTransactionsAccountsObsCollBoolTextCouple(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            if (handledRequest.Accounts.Count > 0)
            {
                var accs =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                            && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                        group r.Attribute(Config.AuftragsKontoField).Value by r.Attribute(Config.AuftragsKontoField).Value into g
                        select new BoolTextCouple(!handledRequest.Accounts.Contains(g.Key), g.Key);
                return new ObservableCollection<BoolTextCouple>(accs.Distinct<BoolTextCouple>());
            }
            else
            {
                try
                {
                    var accs =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                            && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
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

        protected ObservableCollection<BoolTextCouple> GetBuchungstextOverDateRange(DataRequest request)
        {
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            if (handledRequest.Buchungstexts.Count > 0)
            {
                try
                {
                    var res =
                        from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                            && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                        group r.Element(Config.WertDatumField).Value by r.Element(Config.BuchungsTextField).Value into g

                        select new BoolTextCouple(!handledRequest.Buchungstexts.Contains(g.Key), g.Key);

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
                        where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                            && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                            && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
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

        protected List<string> ConvertObsCollBoolTextCoupleToList(ObservableCollection<BoolTextCouple> values)
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
            PreprocessedDataRequest handledRequest = HandleRequest(request);
            try
            {
                var res =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= handledRequest.BeginDate
                                        && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= handledRequest.FinishDate
                                        && handledRequest.SelectedRemittee.Equals(r.Element(Config.BeguenstigterField).Value)
                                        && !handledRequest.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) <= -handledRequest.ExpencesHighestValue
                                        && ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= -handledRequest.ExpencesLowestValue
                                        &&
                                        (
                                        r.Element(Config.BeguenstigterField).Value.Contains(String.IsNullOrEmpty(handledRequest.ToFind) ? r.Element(Config.BeguenstigterField).Value : handledRequest.ToFind)
                                        || r.Element(Config.VerwendZweckField).Value.Contains(String.IsNullOrEmpty(handledRequest.ToFind) ? r.Element(Config.VerwendZweckField).Value : handledRequest.ToFind)
                                        )
                                        && !handledRequest.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    orderby DateTime.Parse(r.Element(Config.WertDatumField).Value)

                    select new KeyValuePair<string, decimal>(r.Element(Config.WertDatumField).Value,
                          (-ConvertStringToDecimal(r.Element(Config.BetragField).Value)));


                return res.ToList<KeyValuePair<string, decimal>>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetDates4RemiteeOverDateRange**",
                    Config.AppName + ": Unable to get Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public List<string> GetUserAccounts()
        {
            try
            {
                var accs =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField) // .Elements
                    select r.Attribute(Config.AuftragsKontoField).Value;
                return accs.Distinct<string>().ToList<string>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BuisenessLogicSSKA-GetUserAccounts**",
                    Config.AppName + ": Unable to get Accounts", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
