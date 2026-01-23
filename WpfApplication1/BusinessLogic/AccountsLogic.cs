using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfApplication1.DAL;

namespace WpfApplication1.BusinessLogic
{
    public class AccountsLogic
    {
        //CsvToXmlSSKA csvToXml;
        //AccountsLogic(CsvToXmlSSKA csvToXml)
        //{
        //    this.csvToXml = csvToXml;
        //}
        public static string BankAccount { get; set; }  // to get from user input, may be missing in the recognized headers
        public List<string> GetUserAccounts()
        {
            try
            {
                var accs =
                    from r in CsvToXmlSSKA.DataSource.DescendantsAndSelf(Config.TransactionField) // .Elements
                    select r.Attribute(Config.AuftragsKontoField).Value;
                return accs.Distinct<string>().ToList<string>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "**BusinessLogicSSKA-GetUserAccounts**",
                    Config.AppName + ": Unable to get Accounts", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
    }
}
