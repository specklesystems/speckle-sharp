using System.Collections;
using System.Collections.Generic;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{

  public readonly struct TraversalContext
  {
    public readonly string? propName;
    public readonly Base? parent;
    public readonly Base current;
    
    public TraversalContext(Base current, string? propName = null, Base? parent = null) : this()
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

    
    public IEnumerable<TraversalContext> Traverse(Base root)
    {
      var stack = new Stack<TraversalContext>();
      stack.Push(new TraversalContext(root));

      while (stack.Count > 0)
      {
        TraversalContext head = stack.Pop();
        yield return head;
        
        Base current = head.current;
        var activeRule = GetActiveRuleOrDefault(current);
        foreach (string childProp in activeRule.MembersToTraverse(current))
        {
          TraverseMember(stack, current[childProp], childProp, current);
        }
      }
    }
    
    private static void TraverseMember(Stack<TraversalContext> stack, object? value, string? memberName = null, Base? parent = null)
    {
      switch (value)
      {
        case Base o:
          stack.Push(new TraversalContext(o, memberName, parent));
          break;
        case IList list:
        {
          foreach (object? obj in list)
          {
            TraverseMember(stack, obj, memberName, parent);
          }
          break;
        }
        case IDictionary dictionary:
        {
          foreach (object? obj in dictionary.Values)
          {
            TraverseMember(stack, obj, memberName, parent);
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
