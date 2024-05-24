using System.Collections.Generic;

namespace Speckle.Core.Models.GraphTraversal;

public static class TraversalContextExtensions
{
  /// <summary>
  /// Walks up the tree, returning <see cref="TraversalContext.PropName"/> values, starting with <paramref name="context"/>,
  /// walking up <see cref="TraversalContext.Parent"/> nodes
  /// </summary>
  /// <param name="context"></param>
  /// <returns></returns>
  public static IEnumerable<string> GetPropertyPath(this TraversalContext context)
  {
    TraversalContext? head = context;
    do
    {
      if (head?.PropName == null)
      {
        break;
      }
      yield return head.PropName;

      head = head.Parent;
    } while (true);
  }

  /// <summary>
  /// Walks up the tree, returning all <typeparamref name="T"/> typed ascendant, starting the <typeparamref name="T"/> closest <paramref name="context"/>,
  /// walking up <see cref="TraversalContext.Parent"/> nodes
  /// </summary>
  /// <param name="context"></param>
  /// <returns></returns>
  public static IEnumerable<T> GetAscendantOfType<T>(this TraversalContext context)
    where T : Base
  {
    TraversalContext? head = context;
    do
    {
      if (head.Current is T c)
      {
        yield return c;
      }
      head = head.Parent;
    } while (head != null);
  }
}
