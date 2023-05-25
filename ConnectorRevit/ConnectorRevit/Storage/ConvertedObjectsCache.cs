using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  internal sealed class ConvertedObjectsCache : IConvertedObjectsCache<Base, Element>
  {
    private Dictionary<string, (Base, List<Element>)> convertedObjects = new();

    public void AddConvertedObjects(Base converted, IList<Element> created)
    {
      convertedObjects[converted.applicationId] = (converted, created.ToList());
    }

    public IEnumerable<Base> GetConvertedObjects()
    {
      foreach (var kvp in convertedObjects)
      {
          yield return kvp.Value.Item1;
      }
    }

    public IEnumerable<Base> GetConvertedObjectsFromCreatedId(string id)
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          if (obj.UniqueId != id) continue;

          yield return kvp.Value.Item1;
          yield break;
        }
      }
    }

    public bool HasConvertedObjectWithId(string id)
    {
      return convertedObjects.ContainsKey(id);
    }

    public IEnumerable<Element> GetCreatedObjects()
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          yield return obj;
        }
      }
    }

    public IEnumerable<Element> GetCreatedObjectsFromConvertedId(string id)
    {
      return convertedObjects[id].Item2;
    }

    public bool HasCreatedObjectWithId(string id)
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          if (obj.UniqueId == id) return true;
        }
      }
      return false;
    }
  }
}
