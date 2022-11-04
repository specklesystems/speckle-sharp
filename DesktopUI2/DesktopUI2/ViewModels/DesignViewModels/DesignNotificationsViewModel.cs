﻿using Speckle.Core.Credentials;
using System.Collections.Generic;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignNotificationsViewModel
  {

    public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();


    public DesignNotificationsViewModel()
    {
      Notifications.Add(new NotificationViewModel { Message = "Pinco Pallimno wants to add you to 'This is a Sample stream!'" });
      Notifications.Add(new NotificationViewModel { Message = "Carlo wants to add you to 'This is a Sample stream!'" });
      Notifications.Add(new NotificationViewModel { Message = "Pinco Pallimno wants to add you to 'This is a Sample stream!'" });

    }


  }


}
