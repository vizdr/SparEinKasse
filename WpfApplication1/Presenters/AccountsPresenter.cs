using System;
using WpfApplication1.BusinessLogic;
using WpfApplication1.DAL;

namespace WpfApplication1
{
    // Presenter
    class AccountsPresenter
    {
        private IViewAccounts _viewAccounts;
        private AccountsLogic al;

        public AccountsPresenter(IViewAccounts viewAccounts, AccountsLogic accountsLogic)
        {
            _viewAccounts = viewAccounts;
            al = accountsLogic;
            RegisterViewAccountsHandlers();
        }

        public void Initialize()
        {
            _viewAccounts.UserAccounts = al.GetUserAccounts();
            _viewAccounts.SelectedAccount = AccountsLogic.BankAccount ?? String.Empty;
        }

        private void RegisterViewAccountsHandlers()
        {
            _viewAccounts.OnAccountsAdd += delegate { AccountsLogic.BankAccount = _viewAccounts.SelectedAccount; };
        }
    }
}
