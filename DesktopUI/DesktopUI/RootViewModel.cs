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
  public class RootViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<ShowNotificationEvent>, IHandle<ApplicationEvent>
  {
    private IWindowManager _windowManager;

    private IViewModelFactory _viewModelFactory;

    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public ConnectorBindings Bindings;

    private string _ViewName;
    public string ViewName
    {
      get => _ViewName;
      set => SetAndNotify(ref _ViewName, value);
    }

    private PackIcon BackIcon = new PackIcon { Kind = PackIconKind.ArrowLeft, Foreground = System.Windows.Media.Brushes.White };

    private PackIcon SettingsIcon = new PackIcon { Kind = PackIconKind.Settings, Foreground = System.Windows.Media.Brushes.Gray };

    private PackIcon _MainButtonIcon;
    public PackIcon MainButtonIcon
    {
      get => _MainButtonIcon;
      set => SetAndNotify(ref _MainButtonIcon, value);
    }

    private bool _MainButton_Checked = false;
    public bool MainButton_Checked
    {
      get => _MainButton_Checked;
      set => SetAndNotify(ref _MainButton_Checked, value);
    }

    public Dictionary<string, IScreen> Pages = new Dictionary<string, IScreen>();

    public RootViewModel(IWindowManager windowManager, IEventAggregator events, IViewModelFactory viewModelFactory, ConnectorBindings bindings)
    {
      _windowManager = windowManager;
      _viewModelFactory = viewModelFactory;
      Bindings = bindings;
      
      DisplayName = "Speckle " + bindings.GetApplicationHostName();
      
      LoadPages();

      ActivateItem(Pages["streams"]);

      ViewName = ActiveItem.DisplayName;
      MainButtonIcon = SettingsIcon;

      events.Subscribe(this);

      Utils.Globals.RVMInstance = this;
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

    // Needs a bit of cleanup.
    public void GoToSettingsOrBack()
    {
      if( ActiveItem is StreamViewModel )
      {
        ActiveItem.RequestClose();
        ActiveItem = Pages["streams"];
        ViewName = ActiveItem.DisplayName;
        MainButtonIcon = SettingsIcon;
        MainButton_Checked = false;
        return;
      }

      ActiveItem = ActiveItem is AllStreamsViewModel ? Pages["settings"] : Pages["streams"];
      ViewName = ActiveItem.DisplayName;

      if(!(ActiveItem is AllStreamsViewModel))
      {
        MainButtonIcon = BackIcon;
        MainButton_Checked = true;
      } 
      else
      {
        MainButtonIcon = SettingsIcon;
        MainButton_Checked = false;
      }
    }

    public void GoToStreamViewPage( StreamViewModel streamItem)
    {
      ActivateItem(streamItem);
      ViewName = ActiveItem.DisplayName;
      MainButtonIcon = BackIcon;
      MainButton_Checked = true;
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
