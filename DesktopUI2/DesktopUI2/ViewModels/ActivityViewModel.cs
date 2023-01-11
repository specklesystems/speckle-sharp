using Avalonia;
using Avalonia.Layout;
using ReactiveUI;
using Speckle.Core.Api;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class ActivityViewModel : ReactiveObject
  {
    public string Message { get; set; }

    public Thickness Margin { get; set; }

    public bool IsReceive { get; set; }
    public string Icon { get; set; }

    public HorizontalAlignment Align { get; set; }

    public Client _client { get; private set; }

    public Task<AccountViewModel> Author => GetAuthorAsync();
    private async Task<AccountViewModel> GetAuthorAsync()
    {
      return await ApiUtils.GetAccount(_activity.userId, _client);
    }

    private ActivityItem _activity { get; set; }

    public ActivityViewModel(ActivityItem item, Client client)
    {
      _activity = item;
      _client = client;

      switch (item.actionType)
      {
        case "commit_receive":
          Margin = new Thickness(50, 10, 10, 0);
          Icon = "ArrowBottomLeft";
          Align = HorizontalAlignment.Right;
          Message = $"received in {item?.info?.sourceApplication} • {Formatting.TimeAgo(item.time)}";
          break;
        case "commit_create":
          Margin = new Thickness(10, 10, 50, 0);
          Icon = "ArrowTopRight";
          Align = HorizontalAlignment.Left;
          Message = $"sent from {item?.info?.commit?.sourceApplication} • {Formatting.TimeAgo(item.time)}";
          break;
        case "stream_create":
          Margin = new Thickness(10, 10, 10, 0);
          Icon = "Pencil";
          Align = HorizontalAlignment.Center;
          Message = $"created this stream • {Formatting.TimeAgo(item.time)}";
          break;
      }

      //since this method is async, the activity might have rendered before the api calls have finished
      //let's make sure teh data is referehsed!
      //this.RaisePropertyChanged("Message");
      //this.RaisePropertyChanged("Margin");
      //this.RaisePropertyChanged("IsReceive");
      //this.RaisePropertyChanged("Icon");
      //this.RaisePropertyChanged("Align");
    }
  }
}
