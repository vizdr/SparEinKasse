using System.Collections.Generic;
using System.Xml.Linq;

namespace WpfApplication1.DAL
{
    public class SSKA_XmlEqualityComparer : IEqualityComparer<XElement>
    {
        public bool Equals(XElement x, XElement y) => x.Value.Equals(y.Value);
        public int GetHashCode(XElement obj) => obj.Value.GetHashCode();
    }
}
