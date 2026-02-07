using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApplication1
{
    interface IViewCharts
    {
        List<KeyValuePair<string, decimal>> Remitties { set;  }
        Decimal AxeRemittiesExpensesMaxValue { set; }
        Decimal AxeExpensesCategoryMaxValue { set; }
        List<KeyValuePair<DateTime, decimal>> Expenses { set; }
        List<KeyValuePair<DateTime, decimal>> Balance { set;}
        ObservableCollection<KeyValuePair<string, decimal>> Incomes { set; }
        List<KeyValuePair<string, string>> ExpensesOverview { set; }
        List<KeyValuePair<string, string>> IncomesOverview { set; }
        List<KeyValuePair<string, decimal>> RemittieeGroups { set; }
        List<KeyValuePair<string, decimal>> ExpensesCategory { set; }
        DateTime BeginDate { get; set; } 
        DateTime EndDate { get; set; }
        string Summary { set; }
        List<string> Accounts { set; }
    }
}
