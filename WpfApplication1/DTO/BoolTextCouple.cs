﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;


namespace WpfApplication1
{
    public class BoolTextCouple : IEqualityComparer<BoolTextCouple> , INotifyPropertyChanged
    {
        private bool isSelected;
        public string Text { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public BoolTextCouple(bool isSelected, string text)
        {
            this.isSelected = isSelected;
            Text = text;
        }

         public bool IsSelected 
         {
             get{ return isSelected; }
             set
             {
                 isSelected = value;
                 OnPropertyChanged(this.Text);
             }
         }

        #region IEqualityComparer<FilterValues> Members

        public bool Equals(BoolTextCouple x, BoolTextCouple y)
        {
            return String.Compare(x.Text, y.Text) == 0;
        }

        public int GetHashCode(BoolTextCouple obj)
        {
            return Text.GetHashCode();
        }

        #endregion
       
        public void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
