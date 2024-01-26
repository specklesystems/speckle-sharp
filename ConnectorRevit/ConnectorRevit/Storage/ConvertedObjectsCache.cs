using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage;

public sealed class ConvertedObjectsCache : IConvertedObjectsCache<Base, Element>
{
  private Dictionary<string, (Base, List<Element>)> convertedObjects = new();

  public void AddConvertedObjects(Base converted, IList<Element> created)
  {
    if (string.IsNullOrEmpty(converted.applicationId))
    {
      return;
    }

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
        if (obj.UniqueId != id)
        {
          continue;
        }

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
    if (convertedObjects.TryGetValue(id, out var value))
    {
      return value.Item2;
    }

    return Enumerable.Empty<Element>();
  }

  public bool HasCreatedObjectWithId(string id)
  {
    foreach (var kvp in convertedObjects)
    {
      foreach (var obj in kvp.Value.Item2)
      {
        if (obj.UniqueId == id)
        {
          return true;
        }
      }
    }
    return false;
  }
}
