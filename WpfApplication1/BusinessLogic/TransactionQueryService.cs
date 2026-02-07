using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using WpfApplication1.DAL;
using WpfApplication1.DTO;

namespace WpfApplication1.BusinessLogic
{
    /// <summary>
    /// Executes financial transaction queries against the XML data source.
    /// Extracted from BusinessLogicSSKA to separate data query logic from orchestration.
    /// </summary>
    internal class TransactionQueryService
    {
        private readonly IDataSourceProvider _dataSourceProvider;
        private readonly PreprocessedDataRequest _request;

        public TransactionQueryService(IDataSourceProvider dataSourceProvider, PreprocessedDataRequest request)
        {
            _dataSourceProvider = dataSourceProvider ?? throw new ArgumentNullException(nameof(dataSourceProvider));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        private IEnumerable<XElement> GetFilteredTransactions()
        {
            return _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                .Where(r => DateTime.Parse(r.Element(Config.WertDatumField).Value) >= _request.BeginDate
                         && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= _request.FinishDate
                         && !_request.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                         && !_request.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value));
        }

        private IEnumerable<KeyValuePair<string, decimal>> ApplyRangeFilter(IEnumerable<KeyValuePair<string, decimal>> res)
        {
            return res.Where(p => p.Value >= _request.ExpensesLowestValue
                               && p.Value <= _request.ExpensesHighestValue);
        }

        private decimal ConvertStringToDecimal(string src)
        {
            try
            {
                return decimal.Parse(src, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                DiagnosticLog.Log("TransactionQueryService", $"ConvertStringToDecimal failed for '{src}': {e.Message}");
                return 0m;
            }
        }

        private bool MatchesSearchFilter(XElement r)
        {
            if (string.IsNullOrEmpty(_request.ToFind))
                return true;

            return r.Element(Config.BeguenstigterField).Value.Contains(_request.ToFind)
                || r.Element(Config.VerwendZweckField).Value.Contains(_request.ToFind);
        }

        private static string TruncateString(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #region Expense Queries

        public List<KeyValuePair<string, decimal>> GetExpensesOverDateRange()
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
                DiagnosticLog.Log("TransactionQueryService", $"GetExpensesOverDateRange: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        public List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeInDateRange()
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
                DiagnosticLog.Log("TransactionQueryService", $"GetExpensesOverRemiteeInDateRange: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        public List<KeyValuePair<string, string>> GetExpensesInfoOverDateRange()
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
                DiagnosticLog.Log("TransactionQueryService", $"GetExpensesInfoOverDateRange: {e.Message}");
                return new List<KeyValuePair<string, string>>();
            }
        }

        public List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeGroupsInDateRange()
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
                DiagnosticLog.Log("TransactionQueryService", $"GetExpensesOverRemiteeGroupsInDateRange: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        public List<KeyValuePair<string, decimal>> GetExpensesAtDate(DataRequest request)
        {
            try
            {
                var res =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) == request.AtDate
                          && !_request.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                          && ConvertStringToDecimal(r.Element(Config.BetragField).Value) < 0
                          && !_request.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r.Element(Config.BetragField).Value by r.Element(Config.BeguenstigterField).Value into g
                    select new KeyValuePair<string, decimal>(g.Key, g.Sum(n => -ConvertStringToDecimal(n)));

                return ApplyRangeFilter(res).ToList();
            }
            catch (Exception e)
            {
                DiagnosticLog.Log("TransactionQueryService", $"GetExpensesAtDate: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        public List<KeyValuePair<string, decimal>> GetExpensesOverCategory()
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
                DiagnosticLog.Log("TransactionQueryService", $"GetExpensesOverCategory: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        public List<KeyValuePair<string, decimal>> GetExpenseBeneficiary4CategoryOverDateRange(DataRequest request)
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
                DiagnosticLog.Log("TransactionQueryService", $"GetExpenseBeneficiary4CategoryOverDateRange: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        public List<KeyValuePair<string, decimal>> GetDates4RemiteeOverDateRange(DataRequest request)
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
                DiagnosticLog.Log("TransactionQueryService", $"GetDates4RemiteeOverDateRange: {e.Message}");
                return new List<KeyValuePair<string, decimal>>();
            }
        }

        #endregion

        #region Income Queries

        public List<KeyValuePair<string, string>> GetIncomesInfoOverDateRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    let amount = ConvertStringToDecimal(r.Element(Config.BetragField).Value)
                    where amount >= _request.IncomesLowestValue
                          && amount <= _request.IncomesHighestValue
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
                DiagnosticLog.Log("TransactionQueryService", $"GetIncomesInfoOverDateRange: {e.Message}");
                return new List<KeyValuePair<string, string>>();
            }
        }

        public ObservableCollection<KeyValuePair<string, decimal>> GetIncomesOverDatesRange()
        {
            try
            {
                var res =
                    from r in GetFilteredTransactions()
                    where ConvertStringToDecimal(r.Element(Config.BetragField).Value) >= 0
                    group r.Element(Config.BetragField).Value by r.Element(Config.WertDatumField).Value into g
                    orderby DateTime.Parse(g.Key)
                    select new KeyValuePair<string, decimal>(g.Key.Substring(5), g.Sum(n => ConvertStringToDecimal(n)));

                var filtered = res.Where(p => p.Value >= _request.IncomesLowestValue
                                           && p.Value <= _request.IncomesHighestValue);

                return new ObservableCollection<KeyValuePair<string, decimal>>(filtered.ToList());
            }
            catch (Exception e)
            {
                DiagnosticLog.Log("TransactionQueryService", $"GetIncomesOverDatesRange: {e.Message}");
                return new ObservableCollection<KeyValuePair<string, decimal>>();
            }
        }

        #endregion

        #region Balance & Summary

        public List<KeyValuePair<DateTime, decimal>> GetBalanceOverDateRange()
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
                        for (var currDate = _request.BeginDate;
                             !_request.FinishDate.Date.Equals(currDate.Date.AddDays(-1));
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
                DiagnosticLog.Log("TransactionQueryService", $"GetBalanceOverDateRange: {e.Message}");
                return new List<KeyValuePair<DateTime, decimal>>();
            }
        }

        public string GetSummary()
        {
            string result = $"Total: {_request.BeginDate:d} - {_request.FinishDate:d} : ";

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
                DiagnosticLog.Log("TransactionQueryService", $"GetSummary: {e.Message}");
            }
            return result;
        }

        #endregion

        #region Account & Filter Queries

        public List<string> GetTransactionsAccounts()
        {
            try
            {
                var accs =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= _request.BeginDate
                          && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= _request.FinishDate
                          && !_request.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                    select r.Attribute(Config.AuftragsKontoField).Value;

                return accs.Distinct().ToList();
            }
            catch (Exception e)
            {
                DiagnosticLog.Log("TransactionQueryService", $"GetTransactionsAccounts: {e.Message}");
                return new List<string>();
            }
        }

        public ObservableCollection<BoolTextCouple> GetTransactionsAccountsObsCollBoolTextCouple()
        {
            try
            {
                var accs =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= _request.BeginDate
                          && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= _request.FinishDate
                          && !_request.Buchungstexts.Contains(r.Element(Config.BuchungsTextField).Value)
                    group r by r.Attribute(Config.AuftragsKontoField).Value into g
                    select new BoolTextCouple(!_request.Accounts.Contains(g.Key), g.Key);

                return new ObservableCollection<BoolTextCouple>(accs.Distinct());
            }
            catch (Exception e)
            {
                DiagnosticLog.Log("TransactionQueryService", $"GetTransactionsAccountsObsCollBoolTextCouple: {e.Message}");
                return new ObservableCollection<BoolTextCouple>();
            }
        }

        public ObservableCollection<BoolTextCouple> GetBuchungstextOverDateRange()
        {
            try
            {
                var res =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
                    where DateTime.Parse(r.Element(Config.WertDatumField).Value) >= _request.BeginDate
                          && DateTime.Parse(r.Element(Config.WertDatumField).Value) <= _request.FinishDate
                          && !_request.Accounts.Contains(r.Attribute(Config.AuftragsKontoField).Value)
                    group r by r.Element(Config.BuchungsTextField).Value into g
                    select new BoolTextCouple(!_request.Buchungstexts.Contains(g.Key), g.Key);

                return new ObservableCollection<BoolTextCouple>(res);
            }
            catch (Exception e)
            {
                DiagnosticLog.Log("TransactionQueryService", $"GetBuchungstextOverDateRange: {e.Message}");
                return new ObservableCollection<BoolTextCouple>();
            }
        }

        #endregion
    }
}
