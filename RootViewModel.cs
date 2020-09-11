using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Feed;
using Speckle.DesktopUI.Inbox;
using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Streams;
using Stylet;

namespace Speckle.DesktopUI
{
  public class RootViewModel : Conductor<IScreen>.Collection.OneActive
  {
    private IWindowManager windowManager;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));
    public RootViewModel(IWindowManager windowManager)
    {
      this.windowManager = windowManager;
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
        new StreamsHomeViewModel(),
        new InboxViewModel(),
        new FeedViewModel(),
        new SettingsViewModel()
      };

      pages.ForEach(Items.Add);

      this.ActiveItem = pages[0];
    }
  }
}