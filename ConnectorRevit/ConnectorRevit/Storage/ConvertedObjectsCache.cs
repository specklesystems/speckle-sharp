using System.Collections.Generic;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  internal sealed class ConvertedObjectsCache : IConvertedObjectsCache
  {
    private Dictionary<string, (Base, List<object>)> convertedObjects = new();
    public void AddReceivedElements(List<object> elements, Base @base)
    {
      convertedObjects[@base.applicationId] = (@base, elements);
    }
    public IEnumerable<string> GetApplicationIds()
    {
      return convertedObjects.Keys;
    }
    public IEnumerable<Base> GetConvertedBaseObjects()
    {
      foreach (var tuple in convertedObjects.Values)
      {
        yield return tuple.Item1;
      }
    }
    public IList<object> GetConvertedObjectsFromApplicationId(string applicationId)
    {
      if (convertedObjects.TryGetValue(applicationId, out var elements))
      {
        return elements.Item2;
      }
      return new List<object>();
    }
    public bool ContainsApplicationId(string applicationId)
    {
      return convertedObjects.ContainsKey(applicationId);
    }

    public IEnumerable<object> GetConvertedObjects()
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          yield return obj;
        }
      }
    }
  }
}
