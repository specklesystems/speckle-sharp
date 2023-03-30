using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
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
    public static DefaultRule Instance => instance ??= new DefaultRule();

    private DefaultRule() { }
    public IEnumerable<string> MembersToTraverse(Base b) => Enumerable.Empty<string>();

    public bool DoesRuleHold(Base o) => true;
  }
}
