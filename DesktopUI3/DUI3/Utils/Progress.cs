using DUI3.Bindings;
using DUI3.Models;

namespace DUI3.Utils;

public static class Progress
{
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
      Status = progress == 1 ? "Completed" : "Converting",
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
