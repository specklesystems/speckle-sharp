using System.Collections.Generic;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.Helpers;

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
