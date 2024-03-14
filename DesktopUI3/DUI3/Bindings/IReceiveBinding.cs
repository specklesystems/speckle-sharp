using System.Collections.Generic;
using DUI3.Models;
using DUI3.Models.Card;
using DUI3.Utils;

namespace DUI3.Bindings;

public interface IReceiveBinding : IBinding
{
  /// <summary>
  /// Instructs the host app to start receiving this model version.
  /// </summary>
  /// <param name="modelCardId"> Model card id</param>
  public void Receive(string modelCardId);

  /// <summary>
  /// Instructs the host app to  cancel the receiving for a given model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void CancelReceive(string modelCardId);
}

public static class ReceiveBindingUiCommands
{
  private const string SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME = "setModelReceiveResult";

  public static void SetModelConversionResult(IBridge bridge, string modelCardId, ReceiveResult receiveResult) =>
    bridge.SendToBrowser(SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME, new { modelCardId, receiveResult });
}

public class ReceiverModelCard : ModelCard
{
  public string SelectedVersionId { get; set; }
  public string LatestVersionId { get; set; }
  public string ProjectName { get; set; }
  public string ModelName { get; set; }
  public bool HasDismissedUpdateWarning { get; set; }
  public ReceiveResult ReceiveResult { get; set; }
}

public class ReceiveResult : DiscriminatedObject
{
  public List<string> BakedObjectIds { get; set; }

  public bool Display { get; set; } = false;

  // TODO/THINK Later: results, reports, etc. ?
}
