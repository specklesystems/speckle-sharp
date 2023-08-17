#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitDocumentAggregateCache
  {
    Document Document { get; }
    IRevitObjectCache<T> GetOrInitializeCacheOfType<T>(Action<IRevitObjectCache<T>> initializer, out bool isExistingCache);
    IRevitObjectCache<T> GetOrInitializeEmptyCacheOfType<T>(out bool isExistingCache);
    IRevitObjectCache<T>? TryGetCacheOfType<T>();
    void Invalidate<T>();
    void InvalidateAll();
  }
}
