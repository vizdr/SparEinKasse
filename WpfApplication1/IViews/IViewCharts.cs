using System;
using System.Collections.Generic;
using System.Windows;

namespace WpfApplication1
{
    interface IViewCharts
    {
        List<KeyValuePair<string, decimal>> Remitties { set;  }
        Decimal AxeRemittiesExpencesMaxValue { set; }
        Decimal AxeExpencesCategoryMaxValue { set; }
        List<KeyValuePair<DateTime, decimal>> Expenses { set; }
        List<KeyValuePair<DateTime, decimal>> Balance { set;}
        List<KeyValuePair<string, decimal>> Incomes { set; }
        List<KeyValuePair<string, string>> ExpensesOverview { set; }
        List<KeyValuePair<string, string>> IncomsOverview { set; }
        List<KeyValuePair<string, decimal>> RemittieeGroups { set; }
        List<KeyValuePair<string, decimal>> ExpensesCategory { set; }
        DateTime BeginDate { get; set; }
        DateTime EndDate { get; set; }
        string Summary { set; }
        List<string> Accounts { set; }
        event RoutedEventHandler OnDateIntervalChanged;
    }
}
