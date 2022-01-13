using System;
using System.Collections.ObjectModel;

namespace WpfApplication1
{
    public class FilterParams
    {
        private static readonly FilterParams instance = new FilterParams();
        public static FilterParams GetInstance()
        {
            return instance;
        }
        public ObservableCollection<BoolTextCouple> BuchungstextValues { private set; get; }
        public ObservableCollection<BoolTextCouple> Accounts { private set; get; }
        public string ExpenciesLessThan { get; private set; }
        public string ExpenciesMoreThan { get; private set; }
        public string IncomesLessThan { get; private set; }
        public string IncomesMoreThan { get; private set; }
        public string ToFind { get; private set; }

        private FilterParams()
        {
            BuchungstextValues = new ObservableCollection<BoolTextCouple>();
            Accounts = new ObservableCollection<BoolTextCouple>();
        }
        public FilterParams(string expenciesLessThan, string expenciesMoreThan, string incomesLessThan,
            string incomesMoreThan, string toFind, ObservableCollection<BoolTextCouple> buchungstextValues, ObservableCollection<BoolTextCouple> accounts)
            : this()
        {
            ExpenciesLessThan = expenciesLessThan ?? String.Empty;
            ExpenciesMoreThan = expenciesMoreThan ?? String.Empty;
            IncomesLessThan = incomesLessThan ?? String.Empty;
            IncomesMoreThan = incomesMoreThan ?? String.Empty;
            ToFind = toFind ?? String.Empty;
            BuchungstextValues = buchungstextValues ?? BuchungstextValues;
            Accounts = accounts ?? Accounts;
        }

        public void ResetValues()
        {
            foreach (BoolTextCouple val in BuchungstextValues)
            {
                val.IsSelected = true;
            }
            foreach (BoolTextCouple val in Accounts)
            {
                val.IsSelected = true;
            }
            ExpenciesLessThan = String.Empty;
            ExpenciesMoreThan = String.Empty;
            IncomesLessThan = String.Empty;
            IncomesMoreThan = String.Empty;
            ToFind = String.Empty;
        }
    }
}
