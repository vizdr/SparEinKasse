using System;
using WpfApplication1.DAL;

namespace WpfApplication1
{
    class AccountsPresenter
    {
        private IViewAccounts _viewAccounts;
        private BuisenessLogicSSKA bl;

        public AccountsPresenter(IViewAccounts viewAccounts)
        {
            _viewAccounts = viewAccounts;
            bl = BuisenessLogicSSKA.GetInstance();
            RegisterViewAccountsHandlers();
        }

        public void Initialize()
        {
            _viewAccounts.UserAccounts = bl.GetUserAccounts();
            _viewAccounts.SelectedAccount = XSimpleElemBuilder.BankAccount ?? String.Empty;
        }

        private void RegisterViewAccountsHandlers()
        {
            _viewAccounts.OnAccountsAdd += delegate { XSimpleElemBuilder.BankAccount = _viewAccounts.SelectedAccount; };
        }
    }
}
