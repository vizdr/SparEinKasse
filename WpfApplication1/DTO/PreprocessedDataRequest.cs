﻿using System;
using System.Collections.Generic;

namespace WpfApplication1.DTO
{
    public class PreprocessedDataRequest
    {
        List<string> buchungsTexts = new List<string>();
        List<string> accounts = new List<string>();
        public List<string> Accounts { get; set; }
        public List<string> Buchungstexts { get; set; }
        public Decimal IncomsLowestValue { get; set; }
        public Decimal IncomsHighestValue { get; set; }
        public Decimal ExpencesLowestValue { get; set; }
        public Decimal ExpencesHighestValue { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime AtDate { get; set; }
        public string SelectedRemittee { get; set; }
        public DateTime FinishDate { get; set; }
        public String ToFind { get; set; }
    }
}
