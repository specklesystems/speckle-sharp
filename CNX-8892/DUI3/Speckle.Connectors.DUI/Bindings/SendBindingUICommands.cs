using System.Collections.Generic;
using Speckle.Connectors.DUI.Bridge;

namespace Speckle.Connectors.DUI.Bindings;

// POC: have put this static back but unsure about it or the IBridge having this responsibility... and the static
public static class SendBindingUICommands
{
  private const string REFRESH_SEND_FILTERS_UI_COMMAND_NAME = "refreshSendFilters";
  private const string SET_MODELS_EXPIRED_UI_COMMAND_NAME = "setModelsExpired";
  private const string SET_MODEL_CREATED_VERSION_ID_UI_COMMAND_NAME = "setModelCreatedVersionId";

  // POC.. the only reasons this needs the bridge is to send? realtionship to these messages and the bridge is unclear
  public static void RefreshSendFilters(IBridge bridge) => bridge.Send(REFRESH_SEND_FILTERS_UI_COMMAND_NAME);

  public static void SetModelsExpired(IBridge bridge, IEnumerable<string> expiredModelIds) =>
    bridge.Send(SET_MODELS_EXPIRED_UI_COMMAND_NAME, expiredModelIds);

  public static void SetModelCreatedVersionId(IBridge bridge, string modelCardId, string versionId) =>
    bridge.Send(SET_MODEL_CREATED_VERSION_ID_UI_COMMAND_NAME, new { modelCardId, versionId });
}
