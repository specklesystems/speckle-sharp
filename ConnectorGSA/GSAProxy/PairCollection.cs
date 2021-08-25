using Speckle.ConnectorGSA.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy
{
  //This is designed to maximise performance when speed of searching values (i.e. as opposed to keys) is important.  It is trying to be faster than a SortedList
  //Note: it assumes both keys and values are unique
  internal class PairCollection<U, V> : IPairCollection<U, V> 
  {
    protected readonly Dictionary<U, V> lefts = new Dictionary<U, V>();
    protected readonly Dictionary<V, U> rights = new Dictionary<V, U>();
    protected readonly object dictLock = new object();

    protected readonly bool uIsNullable;
    protected readonly bool vIsNullable;

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

    public virtual void Add(U u, V v)
    {
      lock (dictLock)
      {
        if (!(uIsNullable && u == null))
        {
          lefts.Add(u, v);
        }
        if (!(vIsNullable && v == null))
        {
          rights.Add(v, u);          
        }
      }
    }

    public virtual void Clear()
    {
      lock (dictLock)
      {
        lefts.Clear();
        rights.Clear();
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
    protected virtual void Remove(U u, V v)
    {
      if (!(uIsNullable && u == null))
      {
        lefts.Remove(u);
      }
      if (!(vIsNullable && v == null))
      {
        rights.Remove(v);
      }
    }
    #endregion
  }
}
