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
        private readonly IDataSourceProvider _dataSourceProvider;

        public AccountsLogic()
        {
            // Parameterless constructor for backward compatibility during DI setup
        }

        public AccountsLogic(IDataSourceProvider dataSourceProvider)
        {
            _dataSourceProvider = dataSourceProvider;
        }

        public static string BankAccount { get; set; }  // to get from user input, may be missing in the recognized headers

        public List<string> GetUserAccounts()
        {
            try
            {
                if (_dataSourceProvider?.DataSource == null)
                    return new List<string>();

                var accs =
                    from r in _dataSourceProvider.DataSource.DescendantsAndSelf(Config.TransactionField)
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
