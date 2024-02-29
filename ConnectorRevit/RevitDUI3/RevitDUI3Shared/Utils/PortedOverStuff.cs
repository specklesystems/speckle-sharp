#nullable enable
using UI = Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;

namespace Speckle.ConnectorRevitDUI3.Utils;

//
// Do note, these are copy-pasted constructs from the dui2 connector.
//

/// <summary>
/// Provides the current <see cref="UI.UIDocument"/> to any dependencies which may need it
/// </summary>
public class UIDocumentProvider
{
  private UI.UIApplication revitApplication;

  public UIDocumentProvider(UI.UIApplication revitApplication)
  {
    this.revitApplication = revitApplication;
  }

  private UI.UIDocument uiDocument;

  public UI.UIDocument Entity
  {
    get => uiDocument ?? revitApplication.ActiveUIDocument;
    set => uiDocument = value;
  }
}



/// <summary>
/// Simple conversion cache to store elements that are retrieved and may be needed again throughout the conversion operation
/// </summary>
public sealed class RevitDocumentAggregateCache : IRevitDocumentAggregateCache
{
  private readonly Dictionary<Type, IRevitObjectCache> objectCaches;
  private readonly UIDocumentProvider uiDocumentProvider;
  public Document Document => uiDocumentProvider.Entity.Document;

  public RevitDocumentAggregateCache(UIDocumentProvider uiDocumentProvider)
  {
    this.uiDocumentProvider = uiDocumentProvider;
    this.objectCaches = new();
  }

  public IRevitObjectCache<T> GetOrInitializeEmptyCacheOfType<T>(out bool isExistingCache)
  {
    return GetOrInitializeCacheOfTypeNullable<T>(null, out isExistingCache);
  }

  public IRevitObjectCache<T> GetOrInitializeCacheOfType<T>(
    Action<IRevitObjectCache<T>> initializer,
    out bool isExistingCache
  )
  {
    return GetOrInitializeCacheOfTypeNullable<T>(initializer, out isExistingCache);
  }

  private IRevitObjectCache<T> GetOrInitializeCacheOfTypeNullable<T>(
    Action<IRevitObjectCache<T>>? initializer,
    out bool isExistingCache
  )
  {
    if (!objectCaches.TryGetValue(typeof(T), out var singleCache))
    {
      isExistingCache = false;
      singleCache = new RevitObjectCache<T>(this);
      if (initializer != null)
      {
        initializer((IRevitObjectCache<T>)singleCache);
      }
      objectCaches.Add(typeof(T), singleCache);
    }
    else
    {
      isExistingCache = true;
    }
    return (IRevitObjectCache<T>)singleCache;
  }

  public IRevitObjectCache<T>? TryGetCacheOfType<T>()
  {
    if (!objectCaches.TryGetValue(typeof(T), out var singleCache))
    {
      return null;
    }
    return singleCache as IRevitObjectCache<T>;
  }

  public void Invalidate<T>()
  {
    objectCaches.Remove(typeof(T));
  }

  public void InvalidateAll()
  {
    objectCaches.Clear();
  }
}

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
