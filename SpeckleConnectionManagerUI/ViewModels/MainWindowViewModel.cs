using System.Linq;
using System.Reactive;
using JetBrains.Annotations;
using ReactiveUI;
using SpeckleConnectionManagerUI.Services;

namespace SpeckleConnectionManagerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ViewModelBase content;
        private Authenticate _authenticate = new Authenticate();
        
        public MainWindowViewModel(Database db)
        {
            Content = List = new ConnectStatusViewModel(db.GetItems());
            ConnectToServer = ReactiveCommand.Create<int>(RunConnectToServer);
        }
        
        private ReactiveCommand<int, Unit> ConnectToServer { get; }

        private ViewModelBase Content
        {
            get => content;
            set => this.RaiseAndSetIfChanged(ref content, value);
        }
        
        public ConnectStatusViewModel List { get; }

        private void RunConnectToServer(int identifier)
        {
            var connectionStatusItem = List.Items.FirstOrDefault(i => i.Identifier == identifier);
            if (connectionStatusItem == null) return;
            
            _authenticate.RedirectToAuthPage($"https://{connectionStatusItem.ServerName}");
            
            connectionStatusItem.Disconnected = false;
            connectionStatusItem.Colour = "Orange";
        }

        public void DeleteAllAuthData()
        {
            Sqlite.DeleteAuthData();
            foreach (var connectionStatusItem in  List.Items)
            {
                connectionStatusItem.Colour = "Red";
                connectionStatusItem.Disconnected = true;
            }
        }
    }
}
