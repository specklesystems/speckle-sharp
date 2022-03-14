using Avalonia;
using Avalonia.Layout;
using ReactiveUI;
using Speckle.Core.Api;

namespace DesktopUI2.ViewModels
{
  public class ActivityViewModel : ReactiveObject
  {
    public string Message { get; set; }

    public Thickness Margin { get; set; }

    public bool IsReceive { get; set; }
    public string Icon { get; set; }

    public HorizontalAlignment Align { get; set; }

    public ActivityViewModel(ActivityItem item)
    {
      switch (item.actionType)
      {
        case "commit_receive":
          Margin = new Thickness(50, 10, 10, 0);
          Icon = "ArrowBottomLeft";
          Align = HorizontalAlignment.Right;
          Message = $"Received in {item.info.sourceApplication} {Formatting.TimeAgo(item.time)}";
          break;
        case "commit_create":
          Margin = new Thickness(10, 10, 50, 0);
          Icon = "ArrowTopRight";
          Align = HorizontalAlignment.Left;
          Message = $"Sent from {item.info.commit.sourceApplication} {Formatting.TimeAgo(item.time)}";
          break;
        case "stream_create":
          Margin = new Thickness(10, 10, 10, 0);
          Icon = "Pencil";
          Align = HorizontalAlignment.Center;
          Message = $"Created {Formatting.TimeAgo(item.time)}";
          break;
      }
    }

    public ActivityViewModel() { }
  }
}
