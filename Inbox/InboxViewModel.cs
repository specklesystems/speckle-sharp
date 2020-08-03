using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.DesktopUI.Inbox
{
    class InboxViewModel : BindableBase
    {
        public InboxViewModel()
        {

            RefreshInboxCommand = new RelayCommand<string>(OnRefreshInbox);
        }

        private ObservableCollection<object> _allNotifications;
        private ObservableCollection<object> _filteredNotifications;
        public ObservableCollection<object> AllNotifications
        {
            get => _allNotifications;
            set => SetProperty(ref _allNotifications, value);
        }
        public ObservableCollection<object> FilteredNotifications
        {
            get => _filteredNotifications;
            set => SetProperty(ref _filteredNotifications, value);
        }

        public RelayCommand<string> RefreshInboxCommand;
        private void OnRefreshInbox(string arg)
        {
            // refresh AllNotifications
        }

    }
}
