using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI
{
  public class RootViewModel : Conductor<IScreen>.Collection.OneActive,
    IHandle<ShowNotificationEvent>,
    IHandle<ApplicationEvent>
  {
    private IWindowManager _windowManager;
    private IViewModelFactory _viewModelFactory;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));
    public ConnectorBindings Bindings;
    public string HostName => Bindings.GetApplicationHostName();

    public RootViewModel(
      IWindowManager windowManager,
      IEventAggregator events,
      IViewModelFactory viewModelFactory,
      ConnectorBindings bindings)
    {
      _windowManager = windowManager;
      _viewModelFactory = viewModelFactory;
      Bindings = bindings;
      LoadPages();
      events.Subscribe(this);

      _darkMode = Properties.Settings.Default.Theme == BaseTheme.Dark;
      ToggleTheme();
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
          //_viewModelFactory.CreateInboxViewModel(),
          //_viewModelFactory.CreateFeedViewModel(),
          _viewModelFactory.CreateSettingsViewModel()
        };

      pages.ForEach(Items.Add);

      ActiveItem = pages[0];
    }

    public void OpenLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    private bool _isPinned = true;
    public bool IsPinned
    {
      get => _isPinned;
      set => SetAndNotify(ref _isPinned, value);
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

    public void Handle(ShowNotificationEvent message)
    {
      Notifications.Enqueue(message.Notification);
    }

    public void Handle(ApplicationEvent message)
    {
      Notifications.Enqueue($"App Event: {message.Type}");
    }

    public void OnClosing(Window sender, CancelEventArgs e)
    {
      e.Cancel = true;
      // refocusing window with `.Show()` should be handled by individual connectors
      // can be accessed through `Application.Current.MainWindow.Show()`
      // TODO maybe change this to be a notify icon? http://www.hardcodet.net/wpf-notifyicon
      sender.Hide();
    }
  }
}
