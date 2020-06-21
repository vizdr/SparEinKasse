using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml.Linq;

namespace WpfApplication1.DAL
{
    public class SSKA_XmlEqualityComparer : IEqualityComparer<XElement> 
    {
         public bool Equals(XElement x, XElement y) 
         {
             return x.Value.Equals(y.Value);
         }

        public int GetHashCode(XElement obj) 
        {
            return obj.Value.GetHashCode();
        }

        
    }
}
