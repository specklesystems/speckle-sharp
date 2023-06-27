using System.Collections.Generic;
using ConverterRevitShared.Classes;

namespace ConverterRevitShared.Cache
{
  /// <summary>
  /// Empty abstract class that enables all the <see cref="SingleObjectCacheImplementation{T}"/>s to be stored in a single dictionary even though they use different generic type arguments
  /// </summary>
  internal abstract class SingleObjectCache
  {

  }

  /// <summary>
  /// Storage of a single type of object in the <see cref="ConversionOperationCache"/>
  /// </summary>
  /// <typeparam name="T"></typeparam>
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
