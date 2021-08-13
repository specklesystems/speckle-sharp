using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.Cache
{
  //This is designed to maximise performance when speed of searching values (i.e. as opposed to keys) is important.  It is trying to be faster than a SortedList
  //Note: it assumes both keys and values are unique
  internal class PairCollection<U, V> : IPairCollection<U, V> where U : IComparable<U> where V : IComparable<V>
  {
    private readonly Dictionary<U, V> lefts = new Dictionary<U, V>();
    private readonly Dictionary<V, U> rights = new Dictionary<V, U>();
    private readonly object dictLock = new object();

    private U maxLeft;
    private V maxRight;

    private readonly bool uIsNullable;
    private readonly bool vIsNullable;

    public PairCollection()
    {
      uIsNullable = (Nullable.GetUnderlyingType(typeof(U)) != null) || (typeof(U) == typeof(String));
      vIsNullable = (Nullable.GetUnderlyingType(typeof(V)) != null) || (typeof(V) == typeof(String));
    }

    public bool ContainsLeft(U u)
    {
      lock (dictLock)
      {
        return lefts.ContainsKey(u);
      }
    }

    public bool ContainsRight(V v)
    {
      lock (dictLock)
      {
        return rights.ContainsKey(v);
      }
    }

    public U MaxLeft() => maxLeft;
    public V MaxRight() => maxRight;

    public List<U> Lefts
    {
      get
      {
        lock (dictLock)
        {
          return lefts.Keys.ToList();
        }
      }
    }

    public List<V> Rights
    {
      get
      {
        lock (dictLock)
        {
          return rights.Keys.ToList();
        }
      }
    }

    public void Add(U u, V v)
    {
      lock (dictLock)
      {
        if (!(uIsNullable && u == null))
        {
          lefts.Add(u, v);
          if (lefts.Count() == 0 || u.CompareTo(maxLeft) > 0)
          {
            maxLeft = u;
          }
        }
        if (!(vIsNullable && v == null))
        {
          rights.Add(v, u);
          if (rights.Count() == 0 || v.CompareTo(maxRight) > 0)
          {
            maxRight = v;
          }
        }
      }
    }

    public void Clear()
    {
      lock (dictLock)
      {
        lefts.Clear();
        rights.Clear();
        maxLeft = default;
        maxRight = default;
      }
    }

    public int Count()
    {
      lock (dictLock)
      {
        return Math.Max(lefts.Count(), rights.Count());
      }
    }

    public bool FindLeft(V v, out U u)
    {
      if (vIsNullable && v == null)
      {
        u = default;
        return false;
      }
      lock (dictLock)
      {
        if (rights.ContainsKey(v))
        {
          u = rights[v];
          return true;
        }
        u = default;
        return false;
      }
    }

    public bool FindRight(U u, out V v)
    {
      if (uIsNullable && u == null)
      {
        v = default;
        return false;
      }
      lock (dictLock)
      {
        if (lefts.ContainsKey(u))
        {
          v = lefts[u];
          return true;
        }
        v = default;
        return false;
      }
    }

    public void RemoveLeft(U u)
    {
      if (uIsNullable && u == null)
      {
        return;
      }

      lock (dictLock)
      {
        if (!lefts.ContainsKey(u) || !FindRight(u, out V v))
        {
          return;
        }
        Remove(u, v);
      }
    }

    public void RemoveRight(V v)
    {
      if (vIsNullable && v == null)
      {
        return;
      }

      lock (dictLock)
      {
        if (!rights.ContainsKey(v) || !FindLeft(v, out U u))
        {
          return;
        }
        Remove(u, v);
      }
    }

    #region inside_lock_private_fns
    private void Remove(U u, V v)
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
    #endregion
  }
}
