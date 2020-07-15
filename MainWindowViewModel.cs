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
            NavCommand = new RelayCommand<string>(OnNav);
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
        private StreamsViewModel _streamsViewModel = new StreamsViewModel();
        private AccountsViewModel _accountsViewModel = new AccountsViewModel();

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

        public RelayCommand<string> NavCommand { get; private set; }
        private void OnNav(string destination)
        {
            switch (destination)
            {
                case "Accounts":
                    CurrentViewModel = _accountsViewModel;
                    break;
                case "Streams":
                default:
                    CurrentViewModel = _streamsViewModel;
                    break;
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
