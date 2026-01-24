using System.Xml.Linq;
using WpfApplication1.DAL;

namespace WpfApplication1.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of IDataSourceProvider for unit testing.
    /// Allows injecting test XML data without file system dependencies.
    /// </summary>
    public class MockDataSourceProvider : IDataSourceProvider
    {
        public XElement DataSource { get; set; }

        /// <summary>
        /// Creates a mock provider with optional test data.
        /// </summary>
        /// <param name="testData">XML data to use, or empty Root element if null.</param>
        public MockDataSourceProvider(XElement testData = null)
        {
            DataSource = testData ?? new XElement("Root");
        }

        /// <summary>
        /// No-op for tests - does not reload from file system.
        /// </summary>
        public void Reload()
        {
            // No-op in tests
        }

        /// <summary>
        /// Helper to create mock with sample transactions.
        /// </summary>
        public static MockDataSourceProvider WithTransactions(params TransactionData[] transactions)
        {
            var root = new XElement("Root");
            foreach (var tx in transactions)
            {
                root.Add(new XElement("Transaction",
                    new XAttribute("Auftragskonto", tx.Account ?? "DE123456"),
                    new XElement("Wertdatum", tx.Date ?? "2024-01-15"),
                    new XElement("Betrag", tx.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement("Beguenstigter", tx.Beneficiary ?? "Test Beneficiary"),
                    new XElement("Buchungstext", tx.BookingText ?? "ÜBERWEISUNG"),
                    new XElement("Verwendungszweck", tx.Purpose ?? "Test Purpose"),
                    new XElement("Category", tx.Category ?? "Uncategorized")
                ));
            }
            return new MockDataSourceProvider(root);
        }
    }

    /// <summary>
    /// Helper class for creating test transaction data.
    /// </summary>
    public class TransactionData
    {
        public string Account { get; set; }
        public string Date { get; set; }
        public decimal Amount { get; set; }
        public string Beneficiary { get; set; }
        public string BookingText { get; set; }
        public string Purpose { get; set; }
        public string Category { get; set; }

        public TransactionData(decimal amount, string beneficiary = null, string date = null)
        {
            Amount = amount;
            Beneficiary = beneficiary;
            Date = date;
        }
    }
}
