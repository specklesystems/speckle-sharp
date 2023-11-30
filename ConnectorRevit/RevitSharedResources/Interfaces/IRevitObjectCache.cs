#nullable enable
using System;
using System.Collections.Generic;

namespace RevitSharedResources.Interfaces;

public interface IRevitObjectCache
{
  IRevitDocumentAggregateCache ParentCache { get; }
  bool ContainsKey(string key);
  ICollection<string> GetAllKeys();
  void Remove(string key);
}

public interface IRevitObjectCache<T> : IRevitObjectCache
{
  T GetOrAdd(string key, Func<T> factory, out bool isExistingValue);
  public T? TryGet(string key);
  ICollection<T> GetAllObjects();
  void Set(string key, T value);
  void AddMany(IEnumerable<T> elements, Func<T, string> keyFactory);
  void AddMany(Dictionary<string, T> elementMap);
}
