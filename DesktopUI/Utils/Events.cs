using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Api;

namespace Speckle.DesktopUI.Utils
{
  public class ShowNotificationEvent
  {
    public string Notification { get; set; }
  }

  public class StreamAddedEvent
  {
    public Stream NewStream { get; set; }
  }
}