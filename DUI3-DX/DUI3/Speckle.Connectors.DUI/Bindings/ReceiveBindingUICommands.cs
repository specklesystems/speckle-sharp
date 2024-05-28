using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;

namespace Speckle.Connectors.DUI.Bindings;

public class ReceiveBindingUICommands : BasicConnectorBindingCommands
{
  // POC: put here events once we needed for receive specific
  private const string SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME = "setModelReceiveResult";

  public ReceiveBindingUICommands(IBridge bridge)
    : base(bridge) { }

  public void SetModelReceiveResult(string modelCardId, IReadOnlyList<ReceiveConversionResult> conversionResults)
  {
    ReceiverModelCardResult res =
      new() { ModelCardId = modelCardId, ReceiveResult = new ReceiveResult(true, conversionResults) };
    Bridge.Send(SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME, res);
  }
}
