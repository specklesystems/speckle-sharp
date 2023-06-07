using System;
using System.Collections.Generic;
using System.Text;
using ConverterRevitShared.Classes;

namespace ConverterRevitShared.Cache
{
  internal abstract class SingleObjectCache
  {

  }

  internal class SingleObjectCacheImplementation<T> : SingleObjectCache
  {
    internal SingleObjectCacheImplementation(ConversionOperationCache cache) 
    {
      cache.AddCacheImplementation(this);
    }
    private readonly Dictionary<string, T> dataStorage = new();

    public bool TryGetValue(string key, out T value)
    {
      return dataStorage.TryGetValue(key, out value);
    }

    public void Add(string key, T value)
    {
      dataStorage.Add(key, value);
    }

    public IEnumerable<T> GetAllObjects()
    {
      return dataStorage.Values;
    }

    public void Invalidate(string key)
    {
      dataStorage.Remove(key);
    }
  }
}
