using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  internal sealed class ConvertedObjectsCache : IConvertedObjectsCache<Base, object>
  {
    private Dictionary<string, (Base, List<object>)> convertedObjects = new();
    public void AddConvertedObject(Base converted, object created)
    {
      convertedObjects[converted.applicationId] = (converted, new List<object>() { created });
    }

    public void AddConvertedObjects(Base converted, IList<object> created)
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
          if (obj is Element el && el.UniqueId != id) continue;

          yield return kvp.Value.Item1;
          yield break;
        }
      }
    }

    public bool HasConvertedObjectWithId(string id)
    {
      return convertedObjects.ContainsKey(id);
    }

    public IEnumerable<object> GetCreatedObjects()
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          yield return obj;
        }
      }
    }

    public IEnumerable<object> GetCreatedObjectsFromConvertedId(string id)
    {
      return convertedObjects[id].Item2;
    }

    public bool HasCreatedObjectWithId(string id)
    {
      foreach (var kvp in convertedObjects)
      {
        foreach (var obj in kvp.Value.Item2)
        {
          if (obj is Element el && el.UniqueId == id) return true;
        }
      }
      return false;
    }
  }
}
