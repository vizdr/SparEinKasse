using System.Xml.Linq;

namespace WpfApplication1.DAL
{
    /// <summary>
    /// Provides access to the XML transaction data source.
    /// Enables unit testing by allowing mock implementations.
    /// </summary>
    public interface IDataSourceProvider
    {
        /// <summary>
        /// Gets the XML element containing all transaction data.
        /// </summary>
        XElement DataSource { get; }

        /// <summary>
        /// Reloads the data source from storage.
        /// </summary>
        void Reload();
    }
}
