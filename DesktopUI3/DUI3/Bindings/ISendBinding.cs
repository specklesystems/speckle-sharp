using System;
using System.Collections.Generic;
using DUI3.Models;
using DUI3.Models.Card;
using DUI3.Utils;
using Speckle.Newtonsoft.Json;

namespace DUI3.Bindings;

public interface ISendBinding : IBinding
{
  public List<ISendFilter> GetSendFilters();

  /// <summary>
  /// Instructs the host app to start sending this model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void Send(string modelCardId);

  /// <summary>
  /// Instructs the host app to  cancel the sending for a given model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void CancelSend(string modelCardId);
}

public static class SendBindingUiCommands
{
  private const string REFRESH_SEND_FILTERS_UI_COMMAND_NAME = "refreshSendFilters";
  private const string SET_MODELS_EXPIRED_UI_COMMAND_NAME = "setModelsExpired";
  private const string SET_MODEL_CREATED_VERSION_ID_UI_COMMAND_NAME = "setModelCreatedVersionId";

  public static void RefreshSendFilters(IBridge bridge) => bridge.SendToBrowser(REFRESH_SEND_FILTERS_UI_COMMAND_NAME);

  public static void SetModelsExpired(IBridge bridge, IEnumerable<string> expiredModelIds) =>
    bridge.SendToBrowser(SET_MODELS_EXPIRED_UI_COMMAND_NAME, expiredModelIds);

  public static void SetModelCreatedVersionId(IBridge bridge, string modelCardId, string versionId) =>
    bridge.SendToBrowser(SET_MODEL_CREATED_VERSION_ID_UI_COMMAND_NAME, new { modelCardId, versionId });
}

public class SenderModelCard : ModelCard
{
  public ISendFilter SendFilter { get; set; }

  [JsonIgnore]
  public HashSet<string> ChangedObjectIds { get; set; } = new();
}

public interface ISendFilter
{
  public string Name { get; set; }
  public string Summary { get; set; }
  public bool IsDefault { get; set; }

  /// <summary>
  /// Gets the ids of the objects targeted by the filter from the host application.
  /// </summary>
  /// <returns></returns>
  public List<string> GetObjectIds();

  /// <summary>
  /// Checks whether any of the targeted objects are affected by changes from the host application.
  /// </summary>
  /// <param name="changedObjectIds"></param>
  /// <returns></returns>
  public bool CheckExpiry(string[] changedObjectIds);
}

public abstract class EverythingSendFilter : DiscriminatedObject, ISendFilter
{
  public string Name { get; set; } = "Everything";
  public string Summary { get; set; } = "All supported objects in the file.";
  public bool IsDefault { get; set; }
  public abstract List<string> GetObjectIds();
  public abstract bool CheckExpiry(string[] changedObjectIds);
}

public abstract class DirectSelectionSendFilter : DiscriminatedObject, ISendFilter
{
  public string Name { get; set; } = "Selection";
  public string Summary { get; set; }
  public bool IsDefault { get; set; }
  public List<string> SelectedObjectIds { get; set; } = new List<string>();
  public abstract List<string> GetObjectIds();
  public abstract bool CheckExpiry(string[] changedObjectIds);
}
