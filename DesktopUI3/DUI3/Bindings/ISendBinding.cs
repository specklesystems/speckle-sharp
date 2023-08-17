using System.Collections.Generic;
using DUI3.Models;
using DUI3.Utils;

namespace DUI3.Bindings;

public interface ISendBinding : IBinding
{
  public List<ISendFilter> GetSendFilters();
  
  /// <summary>
  /// Instructs the host app to start sending this model.
  /// </summary>
  /// <param name="modelId"></param>
  public void Send(string modelId);
  
  /// <summary>
  /// Instructs the host app to  cancel the sending for a given model.
  /// </summary>
  /// <param name="modelId"></param>
  public void CancelSend(string modelId);
  
  /// <summary>
  /// Highlights the objects attached to this sender in the host application.
  /// </summary>
  /// <param name="modelId"></param>
  public void Highlight(string modelId); 
}

public static class SendBindingEvents
{
  public static readonly string FiltersNeedRefresh = "filtersNeedRefresh";
  public static readonly string SendersExpired = "sendersExpired";
  public static readonly string SenderProgress = "senderProgress";
  public static readonly string CreateVersion = "createVersion";
}

public class SenderModelCard : ModelCard
{
  public ISendFilter SendFilter { get; set; }
}

public interface ISendFilter
{
  public string Name { get; set; }
  public string Summary { get; set; }
  
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
  public abstract List<string> GetObjectIds();
  public abstract bool CheckExpiry(string[] changedObjectIds);
}

public abstract class DirectSelectionSendFilter : DiscriminatedObject, ISendFilter
{
  public string Name { get; set; } = "Selection";
  public string Summary { get; set; }
  public List<string> SelectedObjectIds { get; set; } = new List<string>();
  public abstract List<string> GetObjectIds();
  public abstract bool CheckExpiry(string[] changedObjectIds);
}

public class SenderProgress
{
  public string Id { get; set; }
  public string Status { get; set; }
  public int Progress { get; set; }
}

public class CreateVersion
{
  public string AccountId { get; set; }
  public string ModelId { get; set; }
  public string ProjectId { get; set; }
  public string ObjectId { get; set; }
  public string Message { get; set; }
  public string HostApp { get; set; }
}
