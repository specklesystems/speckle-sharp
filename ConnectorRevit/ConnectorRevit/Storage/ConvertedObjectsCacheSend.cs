using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  /// <summary>
  /// Cache of objects that gets built up during the send operation. Every converted and created object
  /// will be stored in this cache
  /// </summary>
  public class ConvertedObjectsCacheSend : IConvertedObjectsCache<Element, Base>
  {
    private Dictionary<string, (Element, List<Base>)> convertedObjects = new();
    public void AddConvertedObjects(Element converted, IList<Base> created)
    {
      if (string.IsNullOrEmpty(converted.UniqueId)) return;
      convertedObjects[converted.UniqueId] = (converted, created.ToList());
    }

    public IEnumerable<Element> GetConvertedObjects()
    {
      foreach (var kvp in convertedObjects)
      {
        yield return kvp.Value.Item1;
      }
    }

    public IEnumerable<Element> GetConvertedObjectsFromCreatedId(string applicationId)
    {
      if (convertedObjects.TryGetValue(applicationId, out var kvp))
      {
        return new List<Element> { kvp.Item1 };
      }
      return Enumerable.Empty<Element>();
    }

    public IEnumerable<Base> GetCreatedObjects()
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          yield return obj;
        }
      }
    }

    public IEnumerable<Base> GetCreatedObjectsFromConvertedId(string id)
    {
      if (convertedObjects.TryGetValue(id, out var kvp))
      {
        return kvp.Item2;
      }
      return Enumerable.Empty<Base>();
    }

    public bool HasConvertedObjectWithId(string id)
    {
      return convertedObjects.ContainsKey(id);
    }

    public bool HasCreatedObjectWithId(string id)
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          if (obj.applicationId == id) return true;
        }
      }
      return false;
    }
  }
}
