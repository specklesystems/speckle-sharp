#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Storage
{
  /// <summary>
  /// Storage of a single type of object in the <see cref="ConversionOperationCache"/>
  /// </summary>
  /// <typeparam name="T"></typeparam>
  internal class RevitObjectCache<T> : IRevitObjectCache<T>
  {
    private readonly Dictionary<string, T> dataStorage;
    public IRevitDocumentAggregateCache ParentCache { get; }

    public RevitObjectCache(IRevitDocumentAggregateCache parentCache)
    {
      ParentCache = parentCache;
      dataStorage = new();
    }

    public T GetOrAdd(string key, Func<T> factory, out bool isExistingValue)
    {
      if (!dataStorage.TryGetValue(key, out var value))
      {
        isExistingValue = false;
        value = factory();
        dataStorage.Add(key, value);
      }
      else
      {
        isExistingValue = true;
      }

      return value;
    }

    public T? TryGet(string key)
    {
      if (!dataStorage.TryGetValue(key, out var value))
      {
        return default(T);
      }

      return value;
    }

    public bool ContainsKey(string key)
    {
      return dataStorage.ContainsKey(key);
    }

    public ICollection<string> GetAllKeys()
    {
      return dataStorage.Keys;
    }
    
    public ICollection<T> GetAllObjects()
    {
      return dataStorage.Values;
    }

    public void Set(string key, T value)
    {
      dataStorage[key] = value;
    }

    public void AddMany(IEnumerable<T> elements, Func<T, string> keyFactory)
    {
      foreach (var element in elements)
      {
        var key = keyFactory(element);
        dataStorage[key] = element;
      }
    }

    public void AddMany(Dictionary<string, T> elementMap)
    {
      foreach (var kvp in elementMap)
      {
        dataStorage[kvp.Key] = kvp.Value;
      }
    }

    public void Remove(string key)
    {
      dataStorage.Remove(key);
    }
  }
}
