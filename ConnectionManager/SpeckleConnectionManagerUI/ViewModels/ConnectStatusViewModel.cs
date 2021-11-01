using System.Collections.Generic;
using System.Collections.ObjectModel;
using ReactiveUI;
using SpeckleConnectionManagerUI.Models;
using SpeckleConnectionManagerUI.Services;

namespace SpeckleConnectionManagerUI.ViewModels
{
    public class ConnectStatusViewModel : ViewModelBase
    {
        public ConnectStatusViewModel(IEnumerable<ConnectStatusItem> items)
        {
            Items = new ObservableCollection<ConnectStatusItem>(items);
        }

        public ConnectStatusViewModel() {}

        public ObservableCollection<ConnectStatusItem> Items { get; }

    }
}