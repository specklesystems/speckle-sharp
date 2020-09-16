using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Api;

namespace Speckle.DesktopUI.Utils
{
  public class ShowNotificationEvent : EventBase
  {
  }

  public class StreamAddedEvent : EventBase
  {
    public Stream NewStream { get; set; }
  }

  public class EventBase
  {
    public string Notification { get; set; }
    public dynamic dynamicInfo { get; set; }
  }
}
