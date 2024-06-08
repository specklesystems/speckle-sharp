using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Caching;

///<inheritdoc/>
public class SendConversionCache : ISendConversionCache
{
  public SendConversionCache() { }

  private Dictionary<(string applicationId, string projectId), ObjectReference> Cache { get; set; } = new(); // NOTE: as this dude's accessed from potentially more operations at the same time, it might be safer to bless him as a concurrent dictionary.

  public void StoreSendResult(string projectId, Dictionary<string, ObjectReference> convertedReferences)
  {
    foreach (var kvp in convertedReferences)
    {
      Cache[(kvp.Key, projectId)] = kvp.Value;
    }
  }

  /// <inheritdoc/>
  public void EvictObjects(IEnumerable<string> objectIds) =>
    Cache = Cache
      .Where(kvp => !objectIds.Contains(kvp.Key.applicationId))
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

  public bool TryGetValue(string projectId, string applicationId, out ObjectReference objectReference) =>
    Cache.TryGetValue((applicationId, projectId), out objectReference);
}
