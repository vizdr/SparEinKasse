using System;
using System.Collections.Generic;

namespace WpfApplication1.DTO
{
    public class PreprocessedDataRequest
    {
        public List<string> Accounts { get; set; }
        public List<string> Buchungstexts { get; set; }
        public Decimal IncomesLowestValue { get; set; }
        public Decimal IncomesHighestValue { get; set; }
        public Decimal ExpensesLowestValue { get; set; }
        public Decimal ExpensesHighestValue { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime AtDate { get; set; }
        public string SelectedRemittee { get; set; }
        public DateTime FinishDate { get; set; }
        public String ToFind { get; set; }
    }
}
