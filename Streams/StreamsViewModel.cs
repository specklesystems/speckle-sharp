using SpeckleDesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpeckleDesktopUI.Streams
{
    class StreamsViewModel : BindableBase
    {
        public StreamsViewModel()
        {
            Account = LocalContext.GetDefaultAccount();
            LoadUserStreams();
            RemoveCommand = new RelayCommand<SpeckleStream>(OnRemove);

        }

        private Account _account;
        public Account Account
        {
            get => _account;
            set => SetProperty(ref _account, value);
        }

        private ObservableCollection<SpeckleStream> _speckleStreams;
        private ObservableCollection<SpeckleStream> _receiverStreams;
        private ObservableCollection<SpeckleStream> _senderStreams;
        public ObservableCollection<SpeckleStream> SpeckleStreams
        {
            get => _speckleStreams;
            set => SetProperty(ref _speckleStreams, value);
        }
        public ObservableCollection<SpeckleStream> ReceiverStreams
        {
            get => _receiverStreams;
            set => SetProperty(ref _receiverStreams, value);
        }
        public ObservableCollection<SpeckleStream> SenderStreams
        {
            get => _senderStreams;
            set => SetProperty(ref _senderStreams, value);
        }

        public RelayCommand<SpeckleStream> RemoveCommand { get; set; }
        private void OnRemove(SpeckleStream stream)
        {
        }

        public void LoadUserStreams()
        {
            var client = new SpeckleApiClient(Account.RestApi, false, "wpf_ui");
            client.AuthToken = Account.Token;

            // dumb data for now as I am getting build errors when I init speckspock

            SenderStreams = new ObservableCollection<SpeckleStream>()
            {
                new SpeckleStream()
                {
                    Name = "Random Stream To Send 👋",
                    Description = "This is a random stream I made today w0w",
                    Owner = "Izzy Lyseggen",
                    Private = false,
                    StreamId = "123streamid",
                    Objects = new List<SpeckleObject>()
                },
                new SpeckleStream()
                {
                    Name = "Another Random Stream 🌵",
                    Description = "This is a random stream I made today w0w",
                    Owner = "Izzy Lyseggen",
                    Private = false,
                    StreamId = "stream123",
                    Objects = new List<SpeckleObject>()
                }
            };
            ReceiverStreams = new ObservableCollection<SpeckleStream>()
            {
                new SpeckleStream()
                {
                    Name = "Random Stream To Receive 🌊",
                    Description = "This is a random stream I made today w0w",
                    Owner = "Izzy Lyseggen",
                    Private = false,
                    StreamId = "anoterstreamid",
                    Objects = new List<SpeckleObject>()
                }
            };
        }
    }
}
