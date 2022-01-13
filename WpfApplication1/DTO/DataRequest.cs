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

        public Boolean DataBankUpdating
        { set { OnDataBankUpdateRequested(); } }

        public DateTime BeginDate
        {
            get
            { return beginDate; }
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
            get
            { return endDate; }
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
            get { return filters; }

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
            get { return atDate; }
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
            get { return selectedRemittee; }
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
            if (DataRequested != null)
                DataRequested(this, EventArgs.Empty);
        }

        protected void OnFilterValuesRequested()
        {
            if (FilterValuesRequested != null)
                FilterValuesRequested(this, EventArgs.Empty);
        }

        protected void OnDataBankUpdateRequested()
        {
            if (DataBankUpdateRequested != null)
                DataBankUpdateRequested(this, EventArgs.Empty);
        }
        protected void OnViewDataRequested()
        {
            if (ViewDataRequested != null)
                ViewDataRequested(this, EventArgs.Empty);
        }

    }
}
