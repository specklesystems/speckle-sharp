using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy
{
  public class TreeNode<T>
  {
    public TreeNode<T> Parent = null;
    public List<TreeNode<T>> Children = new List<TreeNode<T>>();
    public bool IsLeaf { get => Children.Count() == 0; }
    public bool IsRoot { get => Parent == null; }
    public T Value;
  }

  public class TypeTreeCollection<T>
  {
    public List<TreeNode<T>> RootNodes { get => roots.Select(t => nodes[t]).ToList(); }
    public List<TreeNode<T>> LeafNodes { get => leaves.Select(t => nodes[t]).ToList(); }
    public List<T> Errored { get; } = new List<T>();

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

      var currGen = LeafNodes;
      retList.Add(currGen.Select(n => n.Value).ToList());

      bool genAdded;
      do
      {
        genAdded = false;
        var nextGen = new Dictionary<T, TreeNode<T>>();
        foreach (var n in currGen.Where(n => !n.IsRoot))
        {
          var parent = n.Parent.Value;
          if (!nextGen.ContainsKey(parent))
          {
            nextGen.Add(parent, nodes[parent]);
            genAdded = true;
          }
        }
        if (genAdded)
        {
          retList.Add(nextGen.Keys.ToList());
          currGen = nextGen.Values.ToList();
        }
      } while (genAdded);

      return retList;
    }

    public bool Integrate(T parent, IEnumerable<T> children)
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
          if (nodes.ContainsKey(nc) && roots.Contains(nc))
          {
            nodes[parent].Children.Add(nodes[nc]);
            nodes[nc].Parent = nodes[parent];
            roots.Remove(nc);
          }
          else if (!nodes.ContainsKey(nc))
          {
            var node = new TreeNode<T>() { Value = nc, Parent = nodes[parent] };
            nodes[parent].Children.Add(node);
            nodes.Add(nc, node);
            leaves.Add(nc);
          }
          else if (!Errored.Contains(nc))
          {
            Errored.Add(nc);
          }
        }
      }
      return (Errored.Count == 0);
    }
  }
}
