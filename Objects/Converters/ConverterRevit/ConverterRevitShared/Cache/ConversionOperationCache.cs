#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ConverterRevitShared.Cache;

namespace ConverterRevitShared.Classes
{
  internal class ConversionOperationCache
  {
    private readonly Dictionary<Type, SingleObjectCache> objectCaches = new();
    internal void AddCacheImplementation<T>(SingleObjectCacheImplementation<T> typedCache)
    {
      objectCaches.Add(typeof(T), typedCache);
    }
    private SingleObjectCacheImplementation<T>? TryGetCacheOfType<T>()
    {
      if (!objectCaches.TryGetValue(typeof(T), out var cache))
      {
        return null;
      }

      if (cache is not SingleObjectCacheImplementation<T> typedCache)
      {
        throw new Exception("This should never happen (I think)");
      }

      return typedCache;
    }
    private SingleObjectCacheImplementation<T> GetOrCreateCacheOfType<T>()
    {
      var typedCache = TryGetCacheOfType<T>();
      if (typedCache == null)
      {
        typedCache = new SingleObjectCacheImplementation<T>(this);
      }

      return typedCache;
    }

    private void AddMany<T>(SingleObjectCacheImplementation<T> cache, IEnumerable<T> elements, Func<T, string> keyFactory)
    {
      foreach (var el in elements)
      {
        var key = keyFactory(el);
        if (cache.TryGetValue(key, out _)) continue;

        cache.Add(keyFactory(el), el);
      }
    }

    public T GetOrAdd<T>(string key, Func<T> factory, out bool retreived)
    {
      var typedCache = GetOrCreateCacheOfType<T>();

      if (!typedCache.TryGetValue(key, out var value))
      {
        value = factory();
        typedCache.Add(key, value);
        retreived = false;
      }
      else retreived = true;

      return value;
    }

    public IEnumerable<T> GetAllObjectsOfType<T>()
    {
      var typedCache = TryGetCacheOfType<T>();
      if (typedCache == null) return Enumerable.Empty<T>();

      return typedCache.GetAllObjects();
    }
    
    public void InitializeCacheIfNull<T>(Func<IEnumerable<T>> elements, Func<T, string> keyFactory)
    {
      var typedCache = TryGetCacheOfType<T>();
      if (typedCache != null) return;

      typedCache = new SingleObjectCacheImplementation<T>(this);
      AddMany(typedCache, elements(), keyFactory);
    }

    public T? TryGet<T>(string key)
    {
      var typedCache = TryGetCacheOfType<T>();

      if (typedCache == null) return default;

      if (!typedCache.TryGetValue(key, out var value)) return default;

      return value;
    }

    public void AddMany<T>(IEnumerable<T> elements, Func<T, string> keyFactory)
    {
      var typedCache = GetOrCreateCacheOfType<T>();
      AddMany(typedCache, elements, keyFactory);
    }

    public void Invalidate<T>(string? key)
    {
      if (key == null)
      {
        objectCaches.Remove(typeof(T));
        return;
      }

      var cache = TryGetCacheOfType<T>();

      if (cache == null) return;

      cache.Invalidate(key);
    }
  }
}
