using System.Collections.Generic;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.DUI.Utils;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Bindings;

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

  public SendBindingUICommands Commands { get; }
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

public class CreateVersionArgs
{
  public string ModelCardId { get; set; }
  public string ObjectId { get; set; }
}
