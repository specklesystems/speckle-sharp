using Speckle.Connectors.DUI.Bridge;

namespace Speckle.Connectors.DUI.Bindings;

// POC: Send Commands share all commands from BasicBindings + some, this pattern should be revised
public class SendBindingUICommands : BasicConnectorBindingCommands
{
  private const string REFRESH_SEND_FILTERS_UI_COMMAND_NAME = "refreshSendFilters";
  private const string SET_MODELS_EXPIRED_UI_COMMAND_NAME = "setModelsExpired";
  private const string SET_MODEL_CREATED_VERSION_ID_UI_COMMAND_NAME = "setModelCreatedVersionId";

  public SendBindingUICommands(IBridge bridge)
    : base(bridge) { }

  // POC.. the only reasons this needs the bridge is to send? realtionship to these messages and the bridge is unclear
  public void RefreshSendFilters() => Bridge.Send(REFRESH_SEND_FILTERS_UI_COMMAND_NAME);

  public void SetModelsExpired(IEnumerable<string> expiredModelIds) =>
    Bridge.Send(SET_MODELS_EXPIRED_UI_COMMAND_NAME, expiredModelIds);

  public void SetModelCreatedVersionId(string modelCardId, string versionId) =>
    Bridge.Send(SET_MODEL_CREATED_VERSION_ID_UI_COMMAND_NAME, new { modelCardId, versionId });
}
