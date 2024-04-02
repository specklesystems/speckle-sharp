using System.Collections.Generic;
using Speckle.Connectors.DUI.Bridge;

namespace Speckle.Connectors.DUI.Bindings;

public class ReceiveBindingUICommands : BasicConnectorBindingCommands
{
  // POC: put here events once we needed for receive specific
  private const string SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME = "setModelReceiveResult";

  public ReceiveBindingUICommands(IBridge bridge)
    : base(bridge) { }

  public void SetModelReceiveResult(string modelCardId, List<string> bakedObjectIds)
  {
    ReceiverModelCardResult res =
      new()
      {
        ModelCardId = modelCardId,
        ReceiveResult = new ReceiveResult() { BakedObjectIds = bakedObjectIds }
      };
    Bridge.Send(SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME, res);
  }
}
