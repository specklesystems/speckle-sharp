using Avalonia.Controls.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.ViewModels
{
  public class PopUpNotificationViewModel : INotification
  {
    public string Title { get; set; }
    public string Message { get; set; }

    //Can't get these to work when implementing INotification
    public TimeSpan Expiration { get; set; } = TimeSpan.FromSeconds(7);

    public NotificationType Type { get; set; }

    public Action OnClose { get; set; }

    public Action OnClick { get; set; }

    public void ViewCommand()
    {
      OnClick.Invoke();
    }

    public PopUpNotificationViewModel()
    {

    }



  }
}
