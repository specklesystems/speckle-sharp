using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Core.Models;

namespace Speckle.Connectors.DUI.Bindings;

public class ReceiveBindingUICommands : BasicConnectorBindingCommands
{
  // POC: put here events once we needed for receive specific
  private const string SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME = "setModelReceiveResult";

  public ReceiveBindingUICommands(IBridge bridge)
    : base(bridge) { }

  public void SetModelReceiveResult(string modelCardId, IReadOnlyList<ConversionResult> conversionResults)
  {
    ReceiverModelCardResult res =
      new()
      {
        ModelCardId = modelCardId,
        ReceiveResult = new ReceiveResult()
        {
          BakedObjectIds = conversionResults.Where(x => x.IsSuccessful).Select(x => x.ResultId!).ToList()
        }
      };
    Bridge.Send(SET_MODEL_RECEIVE_RESULT_UI_COMMAND_NAME, res);
  }
}
