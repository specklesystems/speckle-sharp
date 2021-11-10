using System;
using System.Net;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using SpeckleConnectionManagerUI.Views;
using SpeckleConnectionManagerUI.Services;

namespace SpeckleConnectionManagerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Title { get => "Speckle@Arup AccountManager "; }
        public string Version { get => $"v{App.RunningVersion}"; }

        public ViewModelBase content;
        private ViewModelBase Content
        {
            get => content;
            set => this.RaiseAndSetIfChanged(ref content, value);
        }

        public string? _NewServerUrl;
        private string? NewServerUrl
        {
            get => _NewServerUrl;
            set => this.RaiseAndSetIfChanged(ref _NewServerUrl, value);
        }

        public Interaction<AddConnectionViewModel, AddConnectionViewModel> ShowNewServerWindowInteraction { get; }
        public ConnectStatusViewModel List { get; }
        private Authenticate _authenticate = new Authenticate();

        private ReactiveCommand<int, Unit> RunConnectToServerCommand { get; }
        private ReactiveCommand<int, Unit> SetDefaultServerCommand { get; }
        private ReactiveCommand<int, Unit> RemoveServerCommand { get; }
        private ReactiveCommand<Unit, Unit> GetNewServerUrlCommand { get; }
        public ReactiveCommand<object, Unit> ShowMsgBoxCommand { get; }

        public MainWindowViewModel() { }

        public MainWindowViewModel(Database db)
        {
            Content = List = new ConnectStatusViewModel(db.GetItems());
            RunConnectToServerCommand = ReactiveCommand.Create<int>(RunConnectToServer);
            SetDefaultServerCommand = ReactiveCommand.Create<int>(SetDefaultServer);
            RemoveServerCommand = ReactiveCommand.Create<int>(RemoveServer);
            GetNewServerUrlCommand = ReactiveCommand.CreateFromTask<Unit, Unit>(GetNewServerUrl_Execute);
            ShowNewServerWindowInteraction = new Interaction<AddConnectionViewModel, AddConnectionViewModel>();
        }

        private void RunConnectToServer(int identifier)
        {
            var connectionStatusItem = List.Items.FirstOrDefault(i => i.Identifier == identifier);
            if (connectionStatusItem == null) return;

            _authenticate.RedirectToAuthPage(connectionStatusItem.ServerUrl);

            connectionStatusItem.Disconnected = false;
            connectionStatusItem.Colour = "Orange";
        }

        private async Task<Unit> GetNewServerUrl_Execute(Unit arg)
        {
            var newServerWindowViewModel = new AddConnectionViewModel();
            var updatedLoginWindowViewModel = await ShowNewServerWindowInteraction.Handle(newServerWindowViewModel);

            if (updatedLoginWindowViewModel != null)
            {
                NewServerUrl = updatedLoginWindowViewModel.NewServerUrl;
                if (NewServerUrl.EndsWith("/")) NewServerUrl = NewServerUrl.Remove(NewServerUrl.Length - 1);
                if (AddConnectionViewModel.CheckServerUrl(NewServerUrl)) AddNewConnection();
            }
            else { }

            return Unit.Default;
        }

        private void SetDefaultServer(int identifier)
        {
            var connectionStatusItem = List.Items.FirstOrDefault(i => i.Identifier == identifier);
            if (connectionStatusItem == null) return;

            connectionStatusItem.Default = true;
            connectionStatusItem.DefaultServerLabel = "DEFAULT";

            Sqlite.SetDefaultServer(connectionStatusItem.ServerUrl, true);

            List.Items.Select(i => i).Where(i => i.Identifier != identifier).ToList().ForEach(i => i.Default = false);
            List.Items.Select(i => i).Where(i => i.Default == false).ToList().ForEach(i => i.DefaultServerLabel = "SET AS DEFAULT");
            List.Items.Select(i => i).Where(i => i.Default == false).ToList().ForEach(i => Sqlite.SetDefaultServer(i.ServerUrl, false));
        }

        public void AddNewConnection()
        {
            var identifier = List.Items.Select(i => i.Identifier).Max() + 1;
            var newItem = new Models.ConnectStatusItem { ServerUrl = NewServerUrl, Identifier = identifier, Disconnected = true, Default = false, DefaultServerLabel = "SET AS DEFAULT", Colour = "Red" };

            List.Items.Add(newItem);

            RunConnectToServer(identifier);
        }

        public void DeleteAllAuthData()
        {
            Sqlite.DeleteAuthData();
            foreach (var connectionStatusItem in List.Items)
            {
                connectionStatusItem.Colour = "Red";
                connectionStatusItem.Disconnected = true;
                connectionStatusItem.Default = false;
                connectionStatusItem.DefaultServerLabel = "SET AS DEFAULT";
            }
        }

        public void RemoveServer(int identifier)
        {
            var connectionStatusItem = List.Items.FirstOrDefault(i => i.Identifier == identifier);
            if (connectionStatusItem == null) return;

            Sqlite.RemoveServer(connectionStatusItem.ServerUrl);

            List.Items.Remove(connectionStatusItem);
        }

        public void LaunchDocs()
        {
            Process.Start("explorer", "https://speckle.arup.com/");
        }
    }
}