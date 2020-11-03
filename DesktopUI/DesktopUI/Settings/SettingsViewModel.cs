using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Settings
{
  public class SettingsViewModel : Screen
  {
    public SettingsViewModel()
    {
      DisplayName = "Settings";
      DefaultAccount = _repo.GetDefault();
      ManageAccountsCommand = new RelayCommand<string>(OnManageAccountsCommand);

      HelpLinks = new List<HelpLink>()
      {
        new HelpLink()
        {
        name = "Docs",
        description = "Browse through the Speckle documentation on our website",
        url = "https://speckle.systems/docs/clients/revit/basics",
        icon = "FileDocument"
        },
        new HelpLink()
        {
        name = "Github",
        description = "Take a look at the source code or submit an issue in the repository",
        url = "https://github.com/specklesystems/DesktopUI",
        icon = "Github"
        },
        new HelpLink()
        {
        name = "Forum",
        description = "Ask questions and join the discussion on our discourse forum",
        url = "https://discourse.speckle.works/",
        icon = "Forum"
        }
      };
    }
    private AccountsRepository _repo = new AccountsRepository();
    public ObservableCollection<Account> LocalAccounts => _repo.LoadAccounts();
    private Account _defaultAccount;
    public Account DefaultAccount
    {
      get => _defaultAccount;
      set => SetAndNotify(ref _defaultAccount, value);
    }
    public List<HelpLink> HelpLinks { get; set; }
    public RelayCommand<string> ManageAccountsCommand { get; set; }

    private void OnManageAccountsCommand(string arg)
    {
      //TODO open Manager app with speckle://
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    public class HelpLink
    {
      public string name { get; set; }
      public string description { get; set; }
      public string url { get; set; }
      public string icon { get; set; }
    }
  }
}
