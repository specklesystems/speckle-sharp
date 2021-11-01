using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy
{
  internal class TreeNode<T>
  {
    internal List<TreeNode<T>> Parents = new List<TreeNode<T>>();
    internal List<TreeNode<T>> Children = new List<TreeNode<T>>();
    internal bool IsLeaf { get => Children.Count() == 0; }
    internal bool IsRoot { get => Parents.Count() == 0; }
    internal T Value;
  }

  public class TypeTreeCollection<T>
  {
    internal List<TreeNode<T>> RootNodes { get => roots.Select(t => nodes[t]).ToList(); }
    internal List<TreeNode<T>> LeafNodes { get => leaves.Select(t => nodes[t]).ToList(); }
    internal List<T> AllValues { get => nodes.Keys.ToList(); }
    internal List<T> Errored { get; } = new List<T>();

    private readonly HashSet<T> validValues;
    private readonly Dictionary<T, TreeNode<T>> nodes = new Dictionary<T, TreeNode<T>>();
    private readonly HashSet<T> leaves = new HashSet<T>();
    private readonly HashSet<T> roots = new HashSet<T>();

    public TypeTreeCollection(IEnumerable<T> validValues)
    {
      this.validValues = new HashSet<T>(validValues);
    }

    public List<List<T>> Generations(bool bottomUp = true)
    {
      var retList = new List<List<T>>();

      var retListFlattened = new List<T>();

      var currGen = RootNodes;
      var addedValues = currGen.Select(n => n.Value).ToList();
      retList.Add(addedValues);
      retListFlattened.AddRange(addedValues);

      bool genAdded;
      do
      {
        genAdded = false;
        var nextGen = new Dictionary<T, TreeNode<T>>();
        foreach (var n in currGen.Where(n => !n.IsLeaf && n.Children != null && n.Children.Count > 0))
        {
          foreach (var c in n.Children.Where(c => c.Parents.All(p => retListFlattened.Contains(p.Value))))
          {
            if (!nextGen.ContainsKey(c.Value))
            {
              nextGen.Add(c.Value, nodes[c.Value]);
              genAdded = true;
            }
          }
        }
        if (genAdded)
        {
          var toAdd = nextGen.Keys.Where(k => !addedValues.Contains(k));
          retList.Add(toAdd.ToList());
          retListFlattened.AddRange(toAdd);
          currGen = nextGen.Values.ToList();
        }
      } while (genAdded);

      retList.Reverse();

      return retList;
    }

    public bool Integrate(T parent, params T[] children)
    {
      if (!validValues.Contains(parent) || !children.All(c => validValues.Contains(c)))
      {
        Errored.Add(parent);
        return false;
      }
      if (nodes.ContainsKey(parent) && leaves.Contains(parent))
      {
        leaves.Remove(parent);
      }
      else if (!nodes.ContainsKey(parent))
      {
        var node = new TreeNode<T> { Value = parent };
        nodes.Add(parent, node);
        roots.Add(parent);
      }

      if (children == null || children.Count() == 0)
      {
        leaves.Add(parent);
      }
      else
      {
        var newChildren = children.Except(nodes[parent].Children.Select(c => c.Value));
        foreach (var nc in newChildren)
        {
          if (nodes.ContainsKey(nc))
          {
            nodes[parent].Children.Add(nodes[nc]);
            nodes[nc].Parents.Add(nodes[parent]);
            if (roots.Contains(nc))
            {
              roots.Remove(nc);
            }
          }
          else
          {
            var node = new TreeNode<T>() { Value = nc };
            node.Parents.Add(nodes[parent]);
            nodes[parent].Children.Add(node);
            nodes.Add(nc, node);
            leaves.Add(nc);
          }
          //else if (!Errored.Contains(nc))
          //{
          //  Errored.Add(nc);
          //}
        }
      }
      return (Errored.Count == 0);
    }

    private bool GetSubtree(T v, ref List<T> values)
    {
      if (nodes.ContainsKey(v))
      {
        values.Add(v);
        if (nodes[v].Children != null && nodes[v].Children.Count > 0)
        {
          foreach (var c in nodes[v].Children.Select(cn => cn.Value))
          {
            if (!GetSubtree(c, ref values))
            {
              return false;
            }
          }
        }
      }
      return true;
    }

    internal bool Remove(params T[] vs)
    {
      foreach (var v in vs)
      {
        if (!nodes.ContainsKey(v))
        {
          continue;
        }
        if (nodes[v].Parents != null)
        {
          foreach (var p in nodes[v].Parents)
          {
            p.Children.Remove(nodes[v]);
          }
        }
        if (leaves.Contains(v))
        {
          leaves.Remove(v);
          nodes.Remove(v);
        }
        else
        {
          var valuesToRemove = new List<T>();
          if (GetSubtree(v, ref valuesToRemove))
          {
            valuesToRemove.ForEach(vtr => nodes.Remove(vtr));
          }
        }
      }
      return true;
    }
  }
}
