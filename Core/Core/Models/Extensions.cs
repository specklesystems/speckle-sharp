using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.Core.Models.Extensions
{
  public static class BaseExtensions
  {
    /// <summary>
    /// Provides access to each base object to determine if they should be included in the output of the 'Flatten' function.
    /// </summary>
    /// <remarks>
    /// Should return 'true' if the object is to be included in the output, 'false' otherwise.
    /// </remarks>
    public delegate bool FlattenPredicate(Base @base);

    /// <summary>
    /// Provides access to each base object in the traverse function, and decides whether the traverse function should continue traversing it's children or not.
    /// </summary>
    /// <remarks>
    /// Should return 'true' if you wish to stop the traverse behaviour, 'false' otherwise.
    /// </remarks>
    public delegate bool BaseRecursionBreaker(Base @base);

    /// <summary>
    /// Traverses through the <paramref name="root"/> object and its children.
    /// Only traverses through the first occurrence of a <see cref="Base"/> object (to prevent infinite recursion on circular references)
    /// </summary>
    /// <param name="root">The root object of the tree to flatten</param>
    /// <param name="recursionBreaker">Optional predicate function to determine whether to break (or continue) traversal of a <see cref="Base"/> object's children.</param>
    /// <returns>A flat List of <see cref="Base"/> objects.</returns>
    /// <seealso cref="Traverse"/>
    public static List<Base> Flatten(this Base root, BaseRecursionBreaker recursionBreaker = null)
    {
      recursionBreaker ??= b => false;
      
      var cache = new HashSet<string>();
      return Traverse(root, b =>
      {
        // Stop if we've already encountered the object and the unique flag is true.
        if (!cache.Add(b.id)) return true;
        
        return recursionBreaker.Invoke(b);
      }).ToList();
    }


    /// <summary>
    /// Depth-first traversal of the specified <paramref name="root"/> object and all of its children as a deferred Enumerable, with a <paramref name="recursionBreaker"/> function to break the traversal.
    /// </summary>
    /// <param name="root">The <see cref="Base"/> object to traverse.</param>
    /// <param name="recursionBreaker">Predicate function to determine whether to break (or continue) traversal of a <see cref="Base"/> object's children.</param>
    /// <returns>Deferred Enumerable of the <see cref="Base"/> objects being traversed (iterable only once).</returns>
    public static IEnumerable<Base> Traverse(this Base root, BaseRecursionBreaker recursionBreaker)
    {
      var stack = new Stack<Base>();
      stack.Push(root);

      while (stack.Count > 0)
      {
        Base current = stack.Pop();
        yield return current;
        
        foreach (string child in current.GetDynamicMemberNames())
        {
          switch (current[child])
          {
            case Base o:
              if(!recursionBreaker(o))
                stack.Push(o);
              break;
            case IDictionary dictionary:
            {
              foreach (object obj in dictionary.Keys)
              {
                if (obj is Base b && !recursionBreaker(b))
                  stack.Push(b);
              }
              break;
            }
            case IList collection:
            {
              foreach (object obj in collection)
              {
                if (obj is Base b && !recursionBreaker(b))
                  stack.Push(b);
              }
              break;
            }
          }
        }
      }
    }
  }
}
