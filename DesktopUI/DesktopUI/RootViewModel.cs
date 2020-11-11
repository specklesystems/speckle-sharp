using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Settings;
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

    public string _ViewName;
    public string ViewName
    {
      get => _ViewName;
      set => SetAndNotify(ref _ViewName, value);
    }

    public Dictionary<string, IScreen> Pages = new Dictionary<string, IScreen>();

    public RootViewModel(IWindowManager windowManager,
      IEventAggregator events,
      IViewModelFactory viewModelFactory,
      ConnectorBindings bindings)
    {
      _windowManager = windowManager;
      _viewModelFactory = viewModelFactory;
      Bindings = bindings;
      DisplayName = "Speckle " + bindings.GetApplicationHostName();
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

      Pages.Add("settings", _viewModelFactory.CreateSettingsViewModel());
      Items.Add(Pages["settings"]);

      Pages.Add("streams", _viewModelFactory.CreateStreamsHomeViewModel());
      Items.Add(Pages["streams"]);

      ActiveItem = Pages["streams"];
    }

    public void OpenLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    public void GoToSettings()
    {
      ActiveItem = ActiveItem is SettingsViewModel ? Pages["streams"] : Pages["settings"];
      ViewName = ActiveItem.DisplayName;
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
