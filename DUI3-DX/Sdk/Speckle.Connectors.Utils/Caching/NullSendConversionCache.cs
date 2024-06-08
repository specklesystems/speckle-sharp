using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Caching;

/// <summary>
/// A null send conversion cache for future use in connectors that cannot support <see cref="ISendConversionCache"/>. It does nothing!
/// </summary>
public class NullSendConversionCache : ISendConversionCache
{
  public void StoreSendResult(string projectId, Dictionary<string, ObjectReference> convertedReferences) { }

  public void EvictObjects(IEnumerable<string> objectIds) { }

  public bool TryGetValue(string projectId, string applicationId, out ObjectReference objectReference)
  {
    objectReference = new ObjectReference();
    return false;
  }
}
