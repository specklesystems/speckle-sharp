using SpeckleDesktopUI.Accounts;
using SpeckleDesktopUI.Streams;
using SpeckleDesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleDesktopUI
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
                new ViewItem("Streams", new StreamsViewModel()),
                new ViewItem("Accounts", new AccountsViewModel())
            };
        }
    }
}
