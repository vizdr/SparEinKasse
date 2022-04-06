using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplication1.DTO
{
    public class DataRequest
    {
        private static readonly DataRequest instance = new DataRequest();

        private DateTime beginDate;
        private DateTime endDate;

        private DateTime atDate;
        private String selectedRemittee;

        private FilterParams filters;

        public event EventHandler DataRequested;
        public event EventHandler FilterValuesRequested;
        public event EventHandler DataBankUpdateRequested;
        public event EventHandler ViewDataRequested;

        public bool DataBankUpdating
        { set { OnDataBankUpdateRequested(); } }

        public DateTime BeginDate
        {
            get => beginDate;
            set
            {
                if (beginDate != value)
                {
                    beginDate = value;
                    OnDataRequested();
                }
            }
        }
        public DateTime EndDate
        {
            get => endDate;
            set
            {
                if (endDate != value)
                {
                    endDate = value;
                    OnDataRequested();
                }
            }
        }
        public FilterParams Filters
        {
            get => filters;

            set
            {
                if (filters != value)
                {
                    filters = value;
                    OnFilterValuesRequested();
                }
            }
        }

        public DateTime AtDate
        {
            get => atDate;
            set
            {
                if (atDate != value)
                {
                    atDate = value;
                    OnViewDataRequested();
                }
            }
        }
        public String SelectedRemittee
        {
            get => selectedRemittee;
            set
            {
                if (selectedRemittee != value)
                {
                    selectedRemittee = value;
                    OnViewDataRequested();
                }
            }
        }

        public static DataRequest GetInstance()
        {
            return instance;
        }
        private DataRequest()
        {
            BeginDate = DateTime.Now.Date.AddDays(-30);
            EndDate = DateTime.Now.Date;
            filters = FilterParams.GetInstance();
        }

        protected void OnDataRequested()
        {
            DataRequested?.Invoke(this, EventArgs.Empty);
        }

        protected void OnFilterValuesRequested()
        {
            FilterValuesRequested?.Invoke(this, EventArgs.Empty);
        }

        protected void OnDataBankUpdateRequested()
        {
            DataBankUpdateRequested?.Invoke(this, EventArgs.Empty);
        }
        protected void OnViewDataRequested()
        {
            ViewDataRequested?.Invoke(this, EventArgs.Empty);
        }

    }
}
