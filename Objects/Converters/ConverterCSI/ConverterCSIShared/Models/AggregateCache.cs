#nullable enable
using System;
using System.Collections.Generic;

namespace ConverterCSIShared.Models
{
  public sealed class AggregateCache
  {
    private readonly Dictionary<Type, SingleObjectTypeCache> objectCaches = new();

    public SingleObjectTypeCache<T> GetOrInitializeEmptyCacheOfType<T>()
    {
      return GetOrInitializeEmptyCacheOfType<T>(out _);
    }
    public SingleObjectTypeCache<T> GetOrInitializeEmptyCacheOfType<T>(out bool isExistingCache)
    {
      return GetOrInitializeCacheOfTypeNullable<T>(null, out isExistingCache);
    }

    public SingleObjectTypeCache<T> GetOrInitializeCacheOfType<T>(Action<SingleObjectTypeCache<T>> initializer)
    {
      return GetOrInitializeCacheOfType<T>(initializer, out _);
    }
    public SingleObjectTypeCache<T> GetOrInitializeCacheOfType<T>(Action<SingleObjectTypeCache<T>> initializer, out bool isExistingCache)
    {
      return GetOrInitializeCacheOfTypeNullable<T>(initializer, out isExistingCache);
    }

    private SingleObjectTypeCache<T> GetOrInitializeCacheOfTypeNullable<T>(Action<SingleObjectTypeCache<T>>? initializer, out bool isExistingCache)
    {
      if (!objectCaches.TryGetValue(typeof(T), out var singleCache))
      {
        isExistingCache = false;
        singleCache = new SingleObjectTypeCache<T>();
        initializer?.Invoke((SingleObjectTypeCache<T>)singleCache);
        objectCaches.Add(typeof(T), singleCache);
      }
      else
      {
        isExistingCache = true;
      }
      return (SingleObjectTypeCache<T>)singleCache;
    }

    public SingleObjectTypeCache<T>? TryGetCacheOfType<T>()
    {
      if (!objectCaches.TryGetValue(typeof(T), out var singleCache))
      {
        return null;
      }
      return singleCache as SingleObjectTypeCache<T>;
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

  public class SingleObjectTypeCache
  {

  }
  public sealed class SingleObjectTypeCache<T> : SingleObjectTypeCache
  {
    private readonly Dictionary<string, T> dataStorage = new();

    public T GetOrAdd(string key, Func<string, T> factory)
    {
      return GetOrAdd(key, factory, out _);
    }
    public T GetOrAdd(string key, Func<string, T> factory, out bool isExistingValue)
    {
      if (!dataStorage.TryGetValue(key, out var value))
      {
        isExistingValue = false;
        value = factory(key);
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
        return default;
      }

      return value;
    }

    public bool ContainsKey(string key)
    {
      return dataStorage.ContainsKey(key);
    }

    public ICollection<string> AllKeys => dataStorage.Keys;

    public ICollection<T> AllObjects => dataStorage.Values;

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
