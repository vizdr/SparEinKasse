using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplication1.DTO
{
    public class DataRequest
    {
        private static readonly DataRequest instance = new DataRequest();

        // beginDate endDate;
        private Tuple<DateTime, DateTime> timeSpan;

        private DateTime atDate;
        private string selectedRemittee;
        private string selectedCategory;

        private FilterViewModel filters;

        public event EventHandler DataRequested;
        public event EventHandler FilterValuesRequested;
        public event EventHandler DataBankUpdateRequested;
        public event EventHandler ViewDataRequested;

        public bool DataBankUpdating
        { 
            set => OnDataBankUpdateRequested(); 
        }

        public Tuple<DateTime, DateTime> TimeSpan
        {
            get => timeSpan;
            set
            {
                if (timeSpan.Item1 != value.Item1 || timeSpan.Item2 != value.Item2)
                {
                    timeSpan = value;
                    OnDataRequested();
                }
            }
        }
        
        public FilterViewModel Filters
        {
            get => filters;

            set
            {
                if (!filters.IsFilterPrepared() || FilterViewModel.isFilterDirty())
                {  
                    OnFilterValuesRequested();
                    FilterViewModel.flopDirty();
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

        public String SelectedCategory
        {
            get => selectedCategory;
            set
            {
                if (selectedCategory != value)
                {
                    selectedCategory = value;
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
            timeSpan = new Tuple<DateTime, DateTime>(DateTime.Now.Date.AddDays(-30), DateTime.Now.Date);
            filters = FilterViewModel.GetInstance();
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
