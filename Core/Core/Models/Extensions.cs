using System;
using System.Collections;
using System.Collections.Concurrent;
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
    /// Traverses through an object and returns it's inner <see cref="Base"/> objects, or a subselection of them.
    /// </summary>
    /// <param name="obj">The object to flatten.</param>
    /// <param name="unique">True if you wish to filter out duplicate objects, false otherwise</param>
    /// <param name="predicate">Determines whether an object should be included in the output.</param>
    /// <param name="recursionBreaker">Determines whether an object should include it's children in the output.</param>
    /// <returns>A flat <see cref="IEnumerable"/> of <see cref="Base"/> objects.</returns>
    public static IEnumerable<Base> Flatten(this Base obj, bool unique, FlattenPredicate predicate = null,
                                            BaseRecursionBreaker recursionBreaker = null)
    {
      // TODO: Cache solution could be improved.
      var result = Enumerable.Empty<Base>();
      var cache = new HashSet<string>();
      obj.Traverse(b =>
      {
        // Stop if we've already encountered the object and the unique flag is true.
        if (unique && cache.Contains(b.id)) return true;
        cache.Add(b.id);
        
        // Add to result if predicate returns true. If no predicate is found, the element will be added by default.
        if (predicate?.Invoke(b) ?? true)
          result = result.Append(b);
        
        // Return the result of the recursionBreaker function.
        // If no recursionBreaker is found, it will continue recurring through the objects children.
        return recursionBreaker?.Invoke(b) ?? false;
      });
      return result;
    }
    
    /// <summary>
    /// Traverses the specified <see cref="Base"/> object, and all of it's children, with a callback to access each of them and break the traverse execution.
    /// </summary>
    /// <param name="obj">The <see cref="Base"/> object to traverse.</param>
    /// <param name="recursionBreaker">Delegate function to access each <see cref="Base"/> object as it's traversed, and allows for control to stop the traverse behaviour on it's children.</param>
    public static void Traverse(this Base obj, BaseRecursionBreaker recursionBreaker) => Traverse((object)obj, recursionBreaker);
    
    /// <summary>
    /// The private implementation of the traverse function for Base objects.
    /// It accepts an <see cref="object"/> to allow recursive calls through lists and dicts.
    /// This was designed to work with <see cref="Base"/> objects,
    /// and you should use the publicly facing version: <see cref="Traverse"/>
    /// </summary>
    /// <param name="obj">The object to traverse</param>
    /// <param name="recursionBreaker">Delegate function to access each <see cref="Base"/> object as it's traversed, and allows for control to stop the traverse behaviour on it's children.</param>
    private static void Traverse(object obj, BaseRecursionBreaker recursionBreaker)
    {
      switch (obj)
      {
        case Base @base:
        {
          // Break if recursionBraker says so. Continue by default.
          if (recursionBreaker?.Invoke(@base) ?? false) break;
          foreach (var prop in @base.GetDynamicMemberNames())
            Traverse(@base[prop], recursionBreaker);
          break;
        }
        case IDictionary dict:
          foreach (var dictValue in dict.Values)
            Traverse(dictValue, recursionBreaker);
          break;
        case IEnumerable enumerable:
          foreach (var listValue in enumerable)
            Traverse(listValue, recursionBreaker);
          break;
      }
    }
  }
}
