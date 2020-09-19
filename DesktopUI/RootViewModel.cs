using System;
using System.Collections.Generic;
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

      private bool _darkMode;
      public bool DarkMode
      {
        get => _darkMode;
        set => SetAndNotify(ref _darkMode, value);
      }

      //
      public void ToggleTheme(bool darkmode)
      {
        PaletteHelper paletteHelper = new PaletteHelper();
        ITheme theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(darkmode ? Theme.Dark : Theme.Light);
        DarkMode = darkmode;
      }

      public void Handle(ShowNotificationEvent message)
      {
        Notifications.Enqueue(message.Notification);
      }

      public void Handle(ApplicationEvent message)
      {
        Notifications.Enqueue($"App Event: {message.Type}");
      }
    }
}
