using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Speckle.Core.Models.GraphTraversal;

public static class TraversalContextExtensions
{
  /// <summary>
  /// Walks up the tree, returning <see cref="TraversalContext.PropName"/> values, starting with <paramref name="context"/>,
  /// walking up <see cref="TraversalContext.Parent"/> nodes
  /// </summary>
  /// <param name="context"></param>
  /// <returns></returns>
  [Pure]
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
  /// Walks up the tree, returning all ascendant, including <paramref name="context"/>
  /// </summary>
  /// <param name="context"></param>
  /// <returns><paramref name="context"/> and all its ascendants</returns>
  [Pure]
  public static IEnumerable<Base> GetAscendants(this TraversalContext context)
  {
    TraversalContext? head = context;
    do
    {
      yield return head.Current;
      head = head.Parent;
    } while (head != null);
  }

  /// <summary>
  /// Walks up the tree, returning all <typeparamref name="T"/> typed ascendant, starting the <typeparamref name="T"/> closest <paramref name="context"/>,
  /// walking up <see cref="TraversalContext.Parent"/> nodes
  /// </summary>
  /// <param name="context"></param>
  /// <returns><paramref name="context"/> and all its ascendants</returns>
  [Pure]
  public static IEnumerable<T> GetAscendantOfType<T>(this TraversalContext context)
    where T : Base
  {
    return context.GetAscendants().OfType<T>();
  }
}
