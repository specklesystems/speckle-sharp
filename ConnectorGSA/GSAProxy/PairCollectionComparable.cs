using System;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy
{
  internal class PairCollectionComparable<U, V> : PairCollection<U,V>, IPairCollectionComparable<U, V> where U : IComparable<U> where V : IComparable<V>
  {
    private U maxLeft;
    private V maxRight;

    public U MaxLeft() => maxLeft;
    public V MaxRight() => maxRight;

    public override void Add(U u, V v)
    {
      lock (dictLock)
      {
        if (!(uIsNullable && u == null))
        {
          if (!lefts.ContainsKey(u))
          {
            lefts.Add(u, v);
            if (lefts.Count() == 0 || u.CompareTo(maxLeft) > 0)
            {
              maxLeft = u;
            }
          }
        }
        if (!(vIsNullable && v == null))
        {
          if (!rights.ContainsKey(v))
          {
            rights.Add(v, u);
            if (rights.Count() == 0 || v.CompareTo(maxRight) > 0)
            {
              maxRight = v;
            }
          }
        }
      }
    }

    protected override void Remove(U u, V v)
    {
      if (!(uIsNullable && u == null))
      {
        lefts.Remove(u);
        if (u.CompareTo(maxLeft) == 0)
        {
          maxLeft = lefts.Count() == 0 ? default : lefts.Keys.Max();
        }
      }
      if (!(vIsNullable && v == null))
      {
        rights.Remove(v);
        if (v.CompareTo(maxRight) == 0)
        {
          maxRight = rights.Count() == 0 ? default : rights.Keys.Max();
        }
      }
    }

    public override void Clear()
    {
      lock (dictLock)
      {
        lefts.Clear();
        rights.Clear();
        maxLeft = default;
        maxRight = default;
      }
    }
  }
}
