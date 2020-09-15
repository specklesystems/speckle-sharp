using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Inbox
{
  public class InboxViewModel : Screen
  {
    public InboxViewModel()
    {
      DisplayName = "Inbox";
      RefreshInboxCommand = new RelayCommand<string>(OnRefreshInbox);
    }

    private ObservableCollection<object> _allNotifications;
    private ObservableCollection<object> _filteredNotifications;
    public ObservableCollection<object> AllNotifications
    {
      get => _allNotifications;
      set => SetAndNotify(ref _allNotifications, value);
    }
    public ObservableCollection<object> FilteredNotifications
    {
      get => _filteredNotifications;
      set => SetAndNotify(ref _filteredNotifications, value);
    }

    public RelayCommand<string> RefreshInboxCommand;
    private void OnRefreshInbox(string arg)
    {
      // refresh AllNotifications
    }

  }
}