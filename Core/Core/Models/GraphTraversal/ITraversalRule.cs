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

  /// <summary>
  /// When <see langword="false"/>,
  /// <see cref="Base"/> objects for which this rule applies,
  /// will be filtered out from the traversal output
  /// (but still traversed normally, as per the <see cref="MembersToTraverse"/>)
  /// </summary>
  /// <remarks>
  /// This property was added to allow for easier filtering of the return of <see cref="GraphTraversal{T}.Traverse(Base)"/>.
  /// Without the option to set some rules as false, it was necessary to duplicate part of the rules in a <see cref="System.Linq.Enumerable.Where{T}(IEnumerable{T},System.Func{T,bool})"/>
  /// </remarks>
  public bool ShouldReturn { get; }
}

/// <summary>
/// The "traverse none" rule that always holds true
/// </summary>
internal sealed class DefaultRule : ITraversalRule
{
  private static DefaultRule? s_instance;

  private DefaultRule() { }

  public static DefaultRule Instance => s_instance ??= new DefaultRule();

  public IEnumerable<string> MembersToTraverse(Base b) => Enumerable.Empty<string>();

  public bool DoesRuleHold(Base o) => true;

  public bool ShouldReturn => true;
}
