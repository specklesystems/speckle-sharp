using System;
using System.Collections.Generic;
using System.Collections;

namespace Speckle.Converters.RevitShared.Services;

public class CachingService : ICachingService
{
  private readonly Dictionary<Type, IDictionary> _objectTypeCaches = new();

  public void AddMany<T>(IEnumerable<T> elements, Func<T, string> keyFactory)
  {
    if (!_objectTypeCaches.TryGetValue(typeof(T), out var objectCache))
    {
      objectCache = new Dictionary<string, T>();
      _objectTypeCaches[typeof(T)] = objectCache;
    }

    Dictionary<string, T> typedObjectCache = (Dictionary<string, T>)objectCache;
    foreach (T element in elements)
    {
      typedObjectCache[keyFactory(element)] = element;
    }
  }

  public T GetOrAdd<T>(string key, Func<T> valueFactory)
  {
    return GetOrAdd<T>(key, (_) => valueFactory(), out _);
  }

  public T GetOrAdd<T>(string key, Func<string, T> valueFactory)
  {
    return GetOrAdd<T>(key, valueFactory, out _);
  }

  public T GetOrAdd<T>(string key, Func<T> valueFactory, out bool isExistingValue)
  {
    return GetOrAdd<T>(key, (_) => valueFactory(), out isExistingValue);
  }

  public T GetOrAdd<T>(string key, Func<string, T> valueFactory, out bool isExistingValue)
  {
    isExistingValue = false;
    T cachedObject;

    if (!_objectTypeCaches.TryGetValue(typeof(T), out var objectCache))
    {
      cachedObject = valueFactory(key);
      objectCache = new Dictionary<string, T>() { { key, cachedObject } };
      _objectTypeCaches[typeof(T)] = objectCache;
      return cachedObject;
    }

    Dictionary<string, T> typedObjectCache = (Dictionary<string, T>)objectCache;
    if (!typedObjectCache.TryGetValue(key, out cachedObject))
    {
      cachedObject = valueFactory(key);
      typedObjectCache.Add(key, cachedObject);
      return cachedObject;
    }

    isExistingValue = true;
    return cachedObject;
  }

  public bool TryGet<T>(string key, out T? cachedObject)
  {
    cachedObject = default;
    if (!_objectTypeCaches.TryGetValue(typeof(T), out var objectCache))
    {
      return false;
    }

    return ((Dictionary<string, T>)objectCache).TryGetValue(key, out cachedObject);
  }

  public void Invalidate<T>()
  {
    _objectTypeCaches.Remove(typeof(T));
  }

  public void InvalidateAll()
  {
    _objectTypeCaches.Clear();
  }
}
