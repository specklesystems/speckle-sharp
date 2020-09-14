using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI
{
  public class RootViewModel : Conductor<IScreen>.Collection.OneActive
  {
    private IWindowManager _windowManager;
    private IViewModelFactory _viewModelFactory;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));
    public RootViewModel(IWindowManager windowManager, IViewModelFactory viewModelFactory)
    {
      _windowManager = windowManager;
      _viewModelFactory = viewModelFactory;
      LoadPages();
    }
    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    private void LoadPages()
    {
      var pages = new List<IScreen>
      {
        _viewModelFactory.CreateStreamsHomeViewModel(),
        _viewModelFactory.CreateInboxViewModel(),
        _viewModelFactory.CreateFeedViewModel(),
        _viewModelFactory.CreateSettingsViewModel()
      };

      pages.ForEach(Items.Add);

      ActiveItem = pages[0];
    }

    public void OpenLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    public void CopyStreamId(string text)
    {
      if (text != null)
      {
        CopyAndNotify(text, "Stream ID copied to clipboard!");
      }
    }

    public void CopyAndNotify(string text, string message)
    {
      Clipboard.SetText(text);
      Notifications.Enqueue(message);
    }

    private bool _darkMode;
    public bool DarkMode
    {
      get => _darkMode;
      set => SetAndNotify(ref _darkMode, value);
    }

    public void ToggleTheme(bool darkmode)
    {
      PaletteHelper paletteHelper = new PaletteHelper();
      ITheme theme = paletteHelper.GetTheme();
      theme.SetBaseTheme(darkmode ? Theme.Dark : Theme.Light);
      DarkMode = darkmode;
    }
  }
}