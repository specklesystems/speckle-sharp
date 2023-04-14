#nullable enable
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
}

/// <summary>
/// The "traverse none" rule that always holds true
/// </summary>
public sealed class DefaultRule : ITraversalRule
{
  private static DefaultRule? instance;

  private DefaultRule() { }

  public static DefaultRule Instance => instance ??= new DefaultRule();

  public IEnumerable<string> MembersToTraverse(Base b)
  {
    return Enumerable.Empty<string>();
  }

  public bool DoesRuleHold(Base o)
  {
    return true;
  }
}
