using Speckle.DesktopUI.Utils;
using Speckle.DesktopUI.Accounts;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.DesktopUI.Settings
{
  class SettingsViewModel : BindableBase
  {
    public SettingsViewModel()
    {
      DefaultAccount = _repo.GetDefault();
      ManageAccountsCommand = new RelayCommand<string>(OnManageAccountsCommand);
    }
    private AccountsRepository _repo = new AccountsRepository();
    private Account _defaultAccount;
    public Account DefaultAccount
    {
      get => _defaultAccount;
      set => SetProperty(ref _defaultAccount, value);
    }

    public RelayCommand<string> ManageAccountsCommand { get; set; }

    private void OnManageAccountsCommand(string arg)
    {

    }
  }
}
