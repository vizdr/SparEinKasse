using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Windows;

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
                            if (!x.Elements(Config.VerwendZweckField).FirstOrDefault().Value.Equals(y.Elements(Config.VerwendZweckField).FirstOrDefault().Value))
                            {
                                // User decides about duplication and excluding of the new entry
                                MessageBoxResult boxResult = MessageBox.Show("Please confirm duplication of payment: " + x.Elements(Config.BetragField).FirstOrDefault().Value + @"\n" + x.Elements(Config.VerwendZweckField).FirstOrDefault().Value + "\n" + y.Elements(Config.VerwendZweckField).FirstOrDefault().Value, "New value is smilar to alt one. Duplication?", MessageBoxButton.YesNo);
                                if (boxResult == MessageBoxResult.No )
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
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


