using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public sealed class GraphTraversal
  {
    private ITraversalRule[] Rules;

    public GraphTraversal(params ITraversalRule[] traversalRule)
    {
      Rules = traversalRule;
    }

    public IEnumerable<Base> Traverse(Base root)
    {
      var stack = new Stack<Base>();
      stack.Push(root);

      while (stack.Count > 0)
      {
        Base current = stack.Pop();
        yield return current;

        foreach (string child in GetMembersToTraverse(root))
        {
          TraverseMember(current[child], stack);
        }
      }
    }

    private static void TraverseMember(object? value, Stack<Base> stack)
    {
      switch (value)
      {
        case Base o:
          stack.Push(o);
          break;
        case IList list:
        {
          foreach (object? obj in list)
          {
            TraverseMember(obj, stack);
          }
          break;
        }
        case IDictionary dictionary:
        {
          foreach (object? obj in dictionary.Values)
          {
            TraverseMember(obj, stack);
          }
          break;
        }
      }
    }

    private IEnumerable<string> GetMembersToTraverse(Base o)
    {
      foreach (var rule in Rules)
      {
        if (rule.DoesRuleHold(o)) return rule.MembersToTraverse(o);
      }

      return Array.Empty<string>();
    }

  }
}
