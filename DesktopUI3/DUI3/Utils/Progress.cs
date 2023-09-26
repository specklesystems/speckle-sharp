using DUI3.Bindings;
using DUI3.Models;

namespace DUI3.Utils;

public static class Progress
{
  public static void Cancel(IBridge bridge, string modelCardId, double? progress = null)
  {
    var args = new ModelProgress()
    {
      Id = modelCardId,
      Status = "Cancelled",
      Progress = progress
    };
    bridge.SendToBrowser(SendBindingEvents.SenderProgress, args);
  }

  /// <summary>
  /// Send serializer progress info to browser
  /// </summary>
  /// <param name="modelCardId"></param>
  /// <param name="progress"></param>
  public static void SerializerProgressToBrowser(IBridge bridge, string modelCardId, double? progress)
  {
    var args = new ModelProgress()
    {
      Id = modelCardId,
      Status = progress == 1 ? "Completed" : "Sending to Server..",
      Progress = progress
    };
    bridge.SendToBrowser(SendBindingEvents.SenderProgress, args);
  }
  
  /// <summary>
  /// Send sender progress info to browser
  /// </summary>
  /// <param name="modelCardId"></param>
  /// <param name="progress"></param>
  public static void SenderProgressToBrowser(IBridge bridge, string modelCardId, double progress)
  {
    var args = new ModelProgress()
    {
      Id = modelCardId,
      Status = progress == 1 ? "Converting Completed" : "Converting",
      Progress = progress
    };
    bridge.SendToBrowser(SendBindingEvents.SenderProgress, args);
  }

  /// <summary>
  /// Send receiver progress info to browser
  /// </summary>
  /// <param name="modelCardId"></param>
  /// <param name="progress"></param>
  public static void ReceiverProgressToBrowser(IBridge bridge, string modelCardId, double progress)
  {
    var args = new ModelProgress()
    {
      Id = modelCardId,
      Status = progress == 1 ? "Completed" : "Constructing",
      Progress = progress
    };
    bridge.SendToBrowser(ReceiveBindingEvents.ReceiverProgress, args);
  }
}
