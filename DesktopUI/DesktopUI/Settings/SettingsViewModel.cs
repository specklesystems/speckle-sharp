using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Settings
{
  public class SettingsViewModel : Screen
  {
    public int LocalAccountsCount => AccountManager.GetAccounts().Count();

    public Account DefaultAccount => AccountManager.GetDefaultAccount();

    public string ConnectorVersion => Globals.HostBindings.ConnectorVersion;

    public string ConnectorName => Globals.HostBindings.ConnectorName;

    public List<HelpLink> HelpLinks { get; }

    public SettingsViewModel()
    {
      DisplayName = "Settings";
      _darkMode = Properties.Settings.Default.Theme == BaseTheme.Inherit;
      ToggleTheme();

      HelpLinks = new List<HelpLink>()
      {
        new HelpLink()
        {
        name = "Docs",
        description = "Browse through the Speckle documentation on our website",
        url = "https://speckle.guide/user/revit.html",
        icon = "FileDocument"
        },
        new HelpLink()
        {
        name = "Github",
        description = "Take a look at the source code or submit an issue in the repository",
        url = "https://github.com/specklesystems/speckle-sharp/",
        icon = "Github"
        },
        new HelpLink()
        {
        name = "Forum",
        description = "Ask questions and join the discussion on our discourse forum",
        url = "https://speckle.community/",
        icon = "Forum"
        }
      };
    }

    protected override void OnActivate()
    {
      base.OnActivate();
      RefreshAccounts();
    }

    public void RefreshAccounts()
    {
      NotifyOfPropertyChange(nameof(LocalAccountsCount));
      NotifyOfPropertyChange(nameof(DefaultAccount));
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
      if (Globals.RootResourceDict != null)
      {
        var theme = Globals.RootResourceDict.GetTheme();
        theme.SetBaseTheme(DarkMode ? Theme.Dark : Theme.Light);
        Globals.RootResourceDict.SetTheme(theme);
      }
      // var paletteHelper = new PaletteHelper();
      // ITheme theme = paletteHelper.GetTheme();
      //
      // theme.SetBaseTheme(DarkMode ? Theme.Dark : Theme.Light);
      // paletteHelper.SetTheme(theme);

      Properties.Settings.Default.Theme = DarkMode ? BaseTheme.Dark : BaseTheme.Light;
      Properties.Settings.Default.Save();
    }
  }
}
