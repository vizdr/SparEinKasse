using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using WpfApplication1.DTO;

namespace WpfApplication1
{
    interface IBuisenessLogic
    {
        List<KeyValuePair<string, decimal>> GetExpensesOverDateRange(DataRequest request);
        List<KeyValuePair<string, decimal>> GetIncomesOverDatesRange(DataRequest request);
        List<KeyValuePair<DateTime, decimal>> GetBalanceOverDateRange(DataRequest request);
        List<KeyValuePair<string, string>> GetExpensesInfoOverDateRange(DataRequest request);
        List<KeyValuePair<string, string>> GetIncomesInfoOverDateRange(DataRequest request);
        List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeInDateRange(DataRequest request);
        List<string> GetTransactionsAccounts(DataRequest request);
        ObservableCollection<BoolTextCouple> GetTransactionsAccountsObsCollBoolTextCouple(DataRequest request);
        string GetSummary(DataRequest request);
        void UpdateData();

        List<KeyValuePair<string, decimal>> GetExpensesOverRemiteeGroupsInDateRange(DataRequest request);
        ObservableCollection<BoolTextCouple> GetBuchungstextOverDateRange(DataRequest request);
    }
}
