using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Settings
{
  public class SettingsViewModel : Screen
  {
    private AccountsRepository _repo = new AccountsRepository();

    public ObservableCollection<Account> LocalAccounts => _repo.LoadAccounts();

    private Account _defaultAccount;

    public Account DefaultAccount
    {
      get => _defaultAccount;
      set => SetAndNotify(ref _defaultAccount, value);
    }

    public List<HelpLink> HelpLinks { get; set; }

    public SettingsViewModel()
    {
      DisplayName = "Settings";
      DefaultAccount = _repo.GetDefault();

      _darkMode = Properties.Settings.Default.Theme == BaseTheme.Dark;
      ToggleTheme();

      HelpLinks = new List<HelpLink>()
      {
        new HelpLink()
        {
        name = "Docs",
        description = "Browse through the Speckle documentation on our website",
        url = "https://speckle.guide/user/connectors.html#revit-rhino",
        icon = "FileDocument"
        },
        new HelpLink()
        {
        name = "Github",
        description = "Take a look at the source code or submit an issue in the repository",
        url = "https://github.com/specklesystems/speckle-sharp/tree/master/DesktopUI",
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

    private bool _darkMode;
    public bool DarkMode
    {
      get => _darkMode;
      set => SetAndNotify(ref _darkMode, value);
    }

    public void ToggleTheme()
    {
      var paletteHelper = new PaletteHelper();
      ITheme theme = paletteHelper.GetTheme();

      theme.SetBaseTheme(DarkMode ? Theme.Dark : Theme.Light);
      paletteHelper.SetTheme(theme);

      Properties.Settings.Default.Theme = DarkMode ? BaseTheme.Dark : BaseTheme.Light;
      Properties.Settings.Default.Save();
    }
  }
}
