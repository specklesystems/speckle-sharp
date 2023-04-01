using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{

  public sealed class TraversalContext
  {
    public readonly string? propName;
    public readonly TraversalContext? parent;
    public readonly Base current;

    public TraversalContext(Base current, string? propName = null, TraversalContext? parent = null)
    {
      this.current = current;
      this.parent = parent;
      this.propName = propName;
    }
  }

  public sealed class GraphTraversal
  {
    private readonly ITraversalRule[] rules;

    public GraphTraversal(params ITraversalRule[] traversalRule)
    {
      rules = traversalRule;
    }

    /// <summary>
    /// Given <paramref name="root"/> object, will recursively traverse members according to the provided traversal rules.
    /// </summary>
    /// <param name="root">The object to traverse members</param>
    /// <returns>Lazily returns <see cref="Base"/> objects found during traversal (including <paramref name="root"/>), wrapped within a <see cref="TraversalContext"/></returns>
    public IEnumerable<TraversalContext> Traverse(Base root)
    {
      var stack = new List<TraversalContext>();
      stack.Add(new TraversalContext(root));

      while (stack.Count > 0)
      {
        int headIndex = stack.Count - 1;
        TraversalContext head = stack[headIndex];
        stack.RemoveAt(headIndex);
        yield return head;

        Base current = head.current;
        var activeRule = GetActiveRuleOrDefault(current);

        foreach (string childProp in activeRule.MembersToTraverse(current))
        {
          TraverseMemberToStack(stack, current[childProp], childProp, head);
        }
      }
    }

    private static void TraverseMemberToStack(ICollection<TraversalContext> stack, object? value, string? memberName = null, TraversalContext? parent = null)
    {
      //test
      switch (value)
      {
        case Base o:
          stack.Add(new TraversalContext(o, memberName, parent));
          break;
        case IList list:
          {
            foreach (object? obj in list)
            {
              TraverseMemberToStack(stack, obj, memberName, parent);
            }
            break;
          }
        case IDictionary dictionary:
          {
            foreach (object? obj in dictionary.Values)
            {
              TraverseMemberToStack(stack, obj, memberName, parent);
            }
            break;
          }
      }
    }



    /// <summary>
    /// Traverses supported Collections yielding <see cref="Base"/> objects.
    /// Does not traverse <see cref="Base"/>, only (potentially nested) collections.
    /// </summary>
    /// <param name="value">The value to traverse</param>
    public static IEnumerable<Base> TraverseMember(object? value)
    {
      //TODO we should benchmark this, as yield returning like this could be suboptimal
      switch (value)
      {
        case Base o:
          yield return o;
          break;
        case IList list:
          {
            foreach (object? obj in list)
            {
              foreach (Base o in TraverseMember(obj))
                yield return o;
            }
            break;
          }
        case IDictionary dictionary:
          {
            foreach (object? obj in dictionary.Values)
            {
              foreach (Base o in TraverseMember(obj))
                yield return o;
            }
            break;
          }
      }
    }

    private ITraversalRule GetActiveRuleOrDefault(Base o)
    {
      return GetActiveRule(o) ?? DefaultRule.Instance;
    }

    private ITraversalRule? GetActiveRule(Base o)
    {
      foreach (var rule in rules)
      {
        if (rule.DoesRuleHold(o)) return rule;
      }

      return null;
    }
  }
}
