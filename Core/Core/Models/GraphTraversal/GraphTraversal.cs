#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace Speckle.Core.Models.GraphTraversal;

public class TraversalContext
{
  public Base current { get; }
  public virtual TraversalContext? parent { get; }
  public string? propName { get; }

  public TraversalContext(Base current, string? propName = null, TraversalContext? parent = default)
    : this(current, propName)
  {
    this.parent = parent;
  }

  protected TraversalContext(Base current, string? propName = null)
  {
    this.current = current;
    this.propName = propName;
  }
}

public class TraversalContext<T> : TraversalContext
  where T : TraversalContext
{
  public override TraversalContext? parent => typedParent;
  public T? typedParent { get; }

  public TraversalContext(Base current, string? propName = null, T? parent = default)
    : base(current, propName)
  {
    this.typedParent = parent;
  }
}

public class GraphTraversal : GraphTraversal<TraversalContext>
{
  public GraphTraversal(params ITraversalRule[] traversalRule)
    : base(traversalRule) { }

  public static readonly string traversalContextId = "traversalContextId";

  protected override TraversalContext NewContext(Base current, string? propName, TraversalContext? parent)
  {
    return new TraversalContext<TraversalContext>(current, propName, parent);
  }
}

public abstract class GraphTraversal<T>
  where T : TraversalContext
{
  private readonly ITraversalRule[] rules;

  protected GraphTraversal(params ITraversalRule[] traversalRule)
  {
    rules = traversalRule;
  }

  /// <summary>
  /// Given <paramref name="root"/> object, will recursively traverse members according to the provided traversal rules.
  /// </summary>
  /// <param name="root">The object to traverse members</param>
  /// <returns>Lazily returns <see cref="Base"/> objects found during traversal (including <paramref name="root"/>), wrapped within a <see cref="TraversalContext"/></returns>
  public IEnumerable<T> Traverse(Base root)
  {
    var stack = new List<T>();
    stack.Add(NewContext(root, null, default));

    while (stack.Count > 0)
    {
      int headIndex = stack.Count - 1;
      T head = stack[headIndex];
      stack.RemoveAt(headIndex);

      yield return head;

      Base current = head.current;
      var activeRule = GetActiveRuleOrDefault(current);

      foreach (string childProp in activeRule.MembersToTraverse(current))
        TraverseMemberToStack(stack, current[childProp], childProp, head);
    }
  }

  private void TraverseMemberToStack(
    ICollection<T> stack,
    object? value,
    string? memberName = null,
    T? parent = default
  )
  {
    //test
    switch (value)
    {
      case Base o:
        stack.Add(NewContext(o, memberName, parent));
        break;
      case IList list:
      {
        foreach (object? obj in list)
          TraverseMemberToStack(stack, obj, memberName, parent);
        break;
      }
      case IDictionary dictionary:
      {
        foreach (object? obj in dictionary.Values)
          TraverseMemberToStack(stack, obj, memberName, parent);
        break;
      }
    }
  }

  protected abstract T NewContext(Base current, string? propName, T? parent);

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
      if (rule.DoesRuleHold(o))
        return rule;

    return null;
  }
}
