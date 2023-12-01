using System;
using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;
using DUI3.Models.Card;

namespace DUI3.Utils;

public static class Notification
{
  public static void ReportReceive(
    IBridge bridge,
    IReadOnlyCollection<string> errors,
    string modelCardId,
    int numberOfObject
  )
  {
    if (errors.Any())
    {
      bridge.SendToBrowser(
        ReceiveBindingEvents.Notify,
        new ModelCardNotification()
        {
          Id = Guid.NewGuid().ToString(),
          ModelCardId = modelCardId,
          Text = $"Speckle objects ({errors.Count}) are not received successfully.",
          Level = "warning",
          Timeout = 5000
        }
      );
    }
    bridge.SendToBrowser(
      ReceiveBindingEvents.Notify,
      new ModelCardNotification()
      {
        Id = Guid.NewGuid().ToString(),
        ModelCardId = modelCardId,
        Text = $"Speckle objects ({numberOfObject - errors.Count}) are received successfully.",
        Level = "success",
        Timeout = 5000
      }
    );
  }
}
