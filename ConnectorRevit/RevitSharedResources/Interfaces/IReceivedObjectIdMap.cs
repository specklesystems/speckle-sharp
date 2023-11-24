#nullable enable
using System.Collections.Generic;

namespace RevitSharedResources.Interfaces;

/// <summary>
/// Objects that implement the IReceivedObjectsCache interface are responsible for
/// reading, querying, mutating, and writing a cache of objects that have been previously received
/// </summary>
public interface IReceivedObjectIdMap<TFrom, TTo>
{
  public void AddConvertedElements(IConvertedObjectsCache<TFrom, TTo> convertedObjects);
  public IEnumerable<string> GetCreatedIdsFromConvertedId(string id);
  public IEnumerable<string> GetAllConvertedIds();
  public void RemoveConvertedId(string id);
}
