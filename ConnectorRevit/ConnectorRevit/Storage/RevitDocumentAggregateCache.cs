#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Storage;

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
