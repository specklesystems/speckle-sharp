using Avalonia;
using Avalonia.Layout;
using ReactiveUI;
using Speckle.Core.Api;
using System.Linq;
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

    public ActivityViewModel()
    {
    }

    public async Task Init(ActivityItem item, Client client)
    {
      var user = "Someone";
      if (HomeViewModel.Instance.Accounts.Any(x => x.Account.userInfo.id == item.userId))
        user = "You";
      else
      {
        var u = await ApiUtils.GetUser(item.userId, client);

        if (u != null)
        {
          user = u.name;
        }
      }


      switch (item.actionType)
      {
        case "commit_receive":
          Margin = new Thickness(50, 10, 10, 0);
          Icon = "ArrowBottomLeft";
          Align = HorizontalAlignment.Right;
          Message = $"{user} received in {item.info.sourceApplication} • {Formatting.TimeAgo(item.time)}";
          break;
        case "commit_create":
          Margin = new Thickness(10, 10, 50, 0);
          Icon = "ArrowTopRight";
          Align = HorizontalAlignment.Left;
          Message = $"{user} sent from {item.info.commit.sourceApplication} • {Formatting.TimeAgo(item.time)}";
          break;
        case "stream_create":
          Margin = new Thickness(10, 10, 10, 0);
          Icon = "Pencil";
          Align = HorizontalAlignment.Center;
          Message = $"{user} created this stream • {Formatting.TimeAgo(item.time)}";
          break;
      }

      //since this method is async, the activity might have rendered before the api calls have finished
      //let's make sure teh data is referehsed!
      this.RaisePropertyChanged("Message");
      this.RaisePropertyChanged("Margin");
      this.RaisePropertyChanged("IsReceive");
      this.RaisePropertyChanged("Icon");
      this.RaisePropertyChanged("Align");
    }

  }
}
