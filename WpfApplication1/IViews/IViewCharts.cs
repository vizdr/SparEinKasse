using System;
using System.Collections.Generic;
using System.Windows;

namespace WpfApplication1
{
    interface IViewCharts
    {
        List<KeyValuePair<string, decimal>> Remitties { set; /* get; */ }
        Decimal AxeRemittiesExpencesMaxValue { set; }
        List<KeyValuePair<DateTime, decimal>> Expenses { set; /* get; */}
        List<KeyValuePair<DateTime, decimal>> Balance { set;/* get; */}
        List<KeyValuePair<string, decimal>> Incomes { set; }
        List<KeyValuePair<string, string>> ExpensesOverview { set; }
        List<KeyValuePair<string, string>> IncomsOverview { set; }
        List<KeyValuePair<string, decimal>> RemittieeGroups { set; /* get; */ }
        DateTime BeginDate { get; set; }
        DateTime EndDate { get; set; }
        string Summary { set; }
        List<string> Accounts { set; }
        event RoutedEventHandler OnDateIntervalChanged;
        //event RoutedEventHandler OnPropertyChanged;
    }
}
