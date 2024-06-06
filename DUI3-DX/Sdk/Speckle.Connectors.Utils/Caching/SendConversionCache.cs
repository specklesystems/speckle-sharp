using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Caching;

/// <summary>
/// <para>Stores object references resulting from a send operation. These can be retrieved back during a subsequent send operation to bypass conversion if
/// they have not been changed. On large sends with small changes, this makes the process much speedier!</para>
/// <para>Note: do not and should not persist between file opens, should just persist in memory between send operations. Always eagerly invalidate.</para>
/// <para>If you ever implement a different conversion cache, do remember that objects in speckle are namespaced to each project (stream). E.g., if you send A to project C and project D, A needs to exist twice in the db. As such, always namespace stored references by project id.</para>
/// <para>Further note: Caching is optional in the send ops; an instance of this should be injected only in applications <b>where we know we can rely on change tracking!</b></para>
/// </summary>
public interface ISendConversionCache
{
  void StoreSendResult(string projectId, Dictionary<string, ObjectReference> convertedReferences);

  /// <summary>
  /// <para>Call this method whenever you need to invalidate a set of objects that have changed in the host app.</para>
  /// <para><b>Failure to do so correctly will result in cache poisoning and incorrect version creation (stale objects).</b></para>
  /// </summary>
  /// <param name="objectIds"></param>
  public void EvictObjects(IEnumerable<string> objectIds);
  bool TryGetValue(string projectId, string applicationId, out ObjectReference objectReference);
}

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
