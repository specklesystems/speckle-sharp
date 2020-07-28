using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Feed;
using Speckle.DesktopUI.Inbox;
using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.DesktopUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            SpeckleCore.SpeckleInitializer.Initialize();

            _viewItems = GetViewItems();
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private ObservableCollection<ViewItem> _viewItems;
        public ObservableCollection<ViewItem> ViewItems
        {
            get => _viewItems;
            set
            {
                _viewItems = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewItems)));
            }
        }

        private BindableBase _currentViewModel;
        public BindableBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentViewModel)));
            }
        }
        private ViewItem _selectedItem;
        public ViewItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value == null || value.Equals(_selectedItem)) return;

                _selectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            }
        }
        private int _selectedIndex;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));
            }
        }

        private ObservableCollection<ViewItem> GetViewItems()
        {
            return new ObservableCollection<ViewItem>
            {
                new ViewItem("Home", new StreamsHomeViewModel()),
                new ViewItem("Inbox", new InboxViewModel()),
                new ViewItem("Feed", new FeedViewModel()),
                new ViewItem("Accounts", new AccountsViewModel()),
                new ViewItem("Settings", new SettingsViewModel())
            };
        }
    }
}
