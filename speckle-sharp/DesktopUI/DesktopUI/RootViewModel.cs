using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI
{
  public class RootViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<ShowNotificationEvent>, IHandle<ApplicationEvent>, IHandle<StreamRemovedEvent>
  {
    private IWindowManager _windowManager;

    private IViewModelFactory _viewModelFactory;

    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public ConnectorBindings Bindings;

    private string _viewName;
    public string ViewName
    {
      get => _viewName;
      set => SetAndNotify(ref _viewName, value);
    }

    private PackIcon BackIcon = new PackIcon { Kind = PackIconKind.ArrowLeft };

    private PackIcon SettingsIcon = new PackIcon { Kind = PackIconKind.Settings };

    private PackIcon _mainButtonIcon;
    public PackIcon MainButtonIcon
    {
      get => _mainButtonIcon;
      set => SetAndNotify(ref _mainButtonIcon, value);
    }

    private bool _mainButton_Checked;
    public bool MainButton_Checked
    {
      get => _mainButton_Checked;
      set => SetAndNotify(ref _mainButton_Checked, value);
    }

    public readonly Dictionary<string, IScreen> Pages = new Dictionary<string, IScreen>();

    public RootViewModel(IWindowManager windowManager, IEventAggregator events, IViewModelFactory viewModelFactory, ConnectorBindings bindings)
    {
      _windowManager = windowManager;
      _viewModelFactory = viewModelFactory;
      Bindings = bindings;

      DisplayName = "Speckle " + bindings.GetHostAppName();

      LoadPages();

      ActivateItem(Pages["streams"]);

      events.Subscribe(this);

      Globals.RVMInstance = this;
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

      Pages.Add("streams", _viewModelFactory.CreateAllStreamsViewModel());
      Items.Add(Pages["streams"]);
    }

    public void OpenLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    public sealed override void ActivateItem(IScreen item)
    {
      base.ActivateItem(item);
      ViewName = item.DisplayName;

      if (ActiveItem is AllStreamsViewModel)
      {
        MainButtonIcon = SettingsIcon;
        MainButton_Checked = false;
      }
      else
      {
        MainButtonIcon = BackIcon;
        MainButton_Checked = true;
      }
    }

    public void GoToSettingsOrBack()
    {
      if (ActiveItem is StreamViewModel)
        ActivateItem(Pages["streams"]);
      else
        ActivateItem(ActiveItem is AllStreamsViewModel ? Pages["settings"] : Pages["streams"]);
    }

    public void GoToStreamViewPage(StreamViewModel streamItem)
    {
      ActivateItem(streamItem);
    }

    public void RefreshActiveView()
    {
      ((SettingsViewModel)Pages["settings"]).Refresh();
      ((AllStreamsViewModel)Pages["streams"]).RefreshPage();
      if (ActiveItem is StreamViewModel streamViewModel)
        streamViewModel.Refresh();

    }

    public void GetHelp()
    {
      System.Diagnostics.Process.Start("https://speckle.community/new-topic?title=I%20need%20help%20with...&category=help&body=Hello+there%21+Some+tips+on+asking+for+help+on+our+forum%3A%0D%0A%0D%0A%2AWhat+happened+and+what+where+you+expecting%3F+Does+it+happen+all+the+times+or+only+some+times%3F+What%27s+the+version+of+this+connector%3F+What+Speckle+server+are+you+using%3F+Can+you+share+with+us+sample+files%2Fstreams+with+instructions+on+how+to+reproduce+it%3F%2A%0D%0A%0D%0AThanks+for+helping+us+make+a+better+Speckle%21");

    }

    public void Handle(StreamRemovedEvent message)
    {
      ActivateItem(Pages["streams"]);
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
