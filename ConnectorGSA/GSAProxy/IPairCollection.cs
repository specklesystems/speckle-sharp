using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy
{
  internal interface IPairCollection<U, V>
  {
    void Add(U u, V v);
    bool ContainsLeft(U u);
    bool ContainsRight(V v);
    void RemoveLeft(U u);
    void RemoveRight(V v);
    bool FindLeft(V v, out U u);
    bool FindRight(U u, out V v);
    void Clear();
    int Count();
    //These are ordered by insertion order
    List<U> Lefts { get; }
    List<V> Rights { get; }
  }
  
  internal interface IPairCollectionComparable<U, V> : IPairCollection<U,V>
  {
    //These methods are quite specific - TODO: review if this needs to be handled differently
    U MaxLeft();
    V MaxRight();
  }
}
