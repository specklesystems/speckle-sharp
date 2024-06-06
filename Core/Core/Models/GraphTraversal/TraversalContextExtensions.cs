using System;
using System.Collections.Generic;
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
    return GetAscendantWhere(context, tc => tc is T).Cast<T>();
  }

  /// <summary>
  /// Walks up the tree, returning all <see cref="Base"/> typed ascendant, starting the <typeparamref name="T"/> closest <paramref name="context"/>,
  /// walking up <see cref="TraversalContext.Parent"/> nodes
  /// </summary>
  /// <param name="context"></param>
  /// <returns></returns>
  public static IEnumerable<Base> GetAscendantWhere(this TraversalContext context, Func<Base, bool> predicate)
  {
    TraversalContext? head = context;
    do
    {
      if (predicate.Invoke(head.Current))
      {
        yield return head.Current;
      }
      head = head.Parent;
    } while (head != null);
  }
}
