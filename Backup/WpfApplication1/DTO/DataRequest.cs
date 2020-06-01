using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplication1.DTO
{
    public class DataRequest
    {
        public DateTime BeginDate {get; set;}
        public DateTime EndDate { get; set; }
        public FilterParams Filters { get; set; }
        public DataRequest()
        {
            BeginDate = DateTime.Now.Date.AddDays(-30);
            EndDate = DateTime.Now.Date;
        }
        public DataRequest(DateTime beginDate, DateTime endDate, FilterParams filters)
        {
            BeginDate = beginDate;
            EndDate = endDate;
            Filters = filters;
        }
    }
}
