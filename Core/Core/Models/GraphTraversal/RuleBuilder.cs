#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Speckle.Core.Models.GraphTraversal;

/// <summary>
/// A traversal rule defines the conditional traversal behaviour when traversing a given <see cref="Base"/> objects.
/// Specifies what members to traverse if any provided <see cref="_conditions"/> are met.
/// </summary>
/// <remarks>Follows the builder pattern to ensure that a rule is complete before usable, see usages</remarks>
public sealed class TraversalRule : ITraversalRule, ITraversalBuilderTraverse
{
  private readonly List<WhenCondition> _conditions;
  private SelectMembers _membersToTraverse;

  private TraversalRule()
  {
    _conditions = new List<WhenCondition>();
  }

  public ITraversalRule ContinueTraversing(SelectMembers membersToTraverse)
  {
    this._membersToTraverse = membersToTraverse;
    return this;
  }

  public ITraversalBuilderTraverse When(WhenCondition condition)
  {
    _conditions.Add(condition);
    return this;
  }

  bool ITraversalRule.DoesRuleHold(Base o)
  {
    foreach (var condition in _conditions)
      if (condition.Invoke(o))
        return true;
    return false;
  }

  IEnumerable<string> ITraversalRule.MembersToTraverse(Base o)
  {
    return _membersToTraverse(o).Distinct(); //TODO distinct is expensive, there may be a better way for us to avoid duplicates
  }

  /// <returns>a new Traversal Rule to be initialised using the Builder Pattern interfaces</returns>
  public static ITraversalBuilderWhen NewTraversalRule()
  {
    return new TraversalRule();
  }
}

#region Builder interfaces/delegates
public delegate bool WhenCondition(Base o);

/// <summary>
/// Interface for traversal rule in a building (unusable) state
/// </summary>
public interface ITraversalBuilderWhen
{
  /// <summary>
  /// Adds a condition to this rule. This rule will hold true when ANY of its conditions holds true.
  /// </summary>
  /// <param name="condition"></param>
  /// <returns>Traversal rule in a building (unusable) state</returns>
  ITraversalBuilderTraverse When(WhenCondition condition);
}

/// <summary>
/// Delegate for selecting members (by member name) of an given <see cref="Base"/> object
/// </summary>
public delegate IEnumerable<string> SelectMembers(Base o);

/// <summary>
/// Interface for traversal rule in a building (unusable) state
/// </summary>
public interface ITraversalBuilderTraverse : ITraversalBuilderWhen
{
  /// <seealso cref="ITraversalRule.MembersToTraverse"/>
  /// <param name="membersToTraverse">Function returning the members that should be traversed for objects where this rule holds <see langword = "true"/></param>
  /// <returns>Traversal rule in a usable state</returns>
  ITraversalRule ContinueTraversing(SelectMembers membersToTraverse);
}
#endregion
