using System.Collections.Generic;
using System.Linq;

namespace Speckle.Core.Models.GraphTraversal;

/// <summary>
/// Interface for a definition of conditional traversal of <see cref="Base"/> objects.
/// </summary>
public interface ITraversalRule
{
  /// <param name="b"></param>
  /// <returns>The member names to traverse</returns>
  /// <remarks>Return may include member names <paramref name="b"/> doesn't have</remarks>
  public IEnumerable<string> MembersToTraverse(Base b);

  /// <summary>
  /// Evaluates the traversal rule given <paramref name="o"/>
  /// </summary>
  /// <param name="o"></param>
  /// <returns></returns>
  public bool DoesRuleHold(Base o);

  ITraversalRule ShouldReturnToOutput(bool shouldReturn = true);
  public bool ShouldReturn { get; }
}

/// <summary>
/// The "traverse none" rule that always holds true
/// </summary>
public sealed class DefaultRule : ITraversalRule
{
  private static DefaultRule? s_instance;

  private DefaultRule() { }

  public static DefaultRule Instance => s_instance ??= new DefaultRule();

  public IEnumerable<string> MembersToTraverse(Base b)
  {
    return Enumerable.Empty<string>();
  }

  public bool DoesRuleHold(Base o)
  {
    return true;
  }

  public ITraversalRule ShouldReturnToOutput(bool shouldReturn = true) => throw new System.NotImplementedException();

  public bool ShouldReturn { get; }
}
