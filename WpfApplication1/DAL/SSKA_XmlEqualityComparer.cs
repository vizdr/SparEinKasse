using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace WpfApplication1.DAL
{
    public class SSKA_XmlEqualityComparer : IEqualityComparer<XElement>
    {
        public bool Equals(XElement x, XElement y)
        {
            // only standard fields
            if (x.Elements(Config.BuchungstagField).FirstOrDefault().Value.Equals( y.Elements(Config.BuchungstagField).FirstOrDefault().Value))
            {
                if (x.Elements(Config.WertDatumField).FirstOrDefault().Value.Equals(y.Elements(Config.WertDatumField).FirstOrDefault().Value))
                {
                    if (x.Elements(Config.BuchungsTextField).FirstOrDefault().Value.Equals(y.Elements(Config.BuchungsTextField).FirstOrDefault().Value))
                    {
                        if (x.Elements(Config.BetragField).FirstOrDefault().Value.Equals(y.Elements(Config.BetragField).FirstOrDefault().Value))
                        {
                            return true;
                        }
                    }
                }
                return true;
            }            
            else
            {                
                return x.Value.Equals(y.Value);
            }
        }
        public int GetHashCode(XElement obj) => obj.Value.GetHashCode();
    }
}


