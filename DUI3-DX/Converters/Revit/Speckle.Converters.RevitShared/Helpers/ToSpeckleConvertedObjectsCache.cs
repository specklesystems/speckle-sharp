using System.Collections.Generic;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: review the cache? should this be a common class?
// Does caching work this way everywhere, i.e, string key and base value?
public sealed class ToSpeckleConvertedObjectsCache
{
  private readonly Dictionary<string, Base> _uniqueIdToConvertedBaseDict = new();

  public void AddConvertedBase(string revitUniqueId, Base b)
  {
    _uniqueIdToConvertedBaseDict.Add(revitUniqueId, b);
  }

  public bool ContainsBaseConvertedFromId(string revitUniqueId)
  {
    return _uniqueIdToConvertedBaseDict.ContainsKey(revitUniqueId);
  }
}
