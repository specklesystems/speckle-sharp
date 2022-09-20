using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.Core.Models.Extensions
{
  public static class BaseExtensions
  {
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
    public static IEnumerable<Base> Flatten(this Base root, BaseRecursionBreaker recursionBreaker = null)
    {
      recursionBreaker ??= b => false;

      var cache = new HashSet<string>();
      var traversal = Traverse(root, b =>
      {
        if (!cache.Add(b.id)) return true;
        
        return recursionBreaker.Invoke(b);
      });
      
      foreach (var b in traversal)
      {
        if (!cache.Contains(b.id)) yield return b;
        //Recursion break will be called after the above
      }
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
        
        if(recursionBreaker(current)) continue;
        
        foreach (string child in current.GetDynamicMemberNames())
        {
          switch (current[child])
          {
            case Base o: 
              stack.Push(o);
              break;
            case IDictionary dictionary:
            {
              foreach (object obj in dictionary.Keys)
              {
                if (obj is Base b)
                  stack.Push(b);
              }
              break;
            }
            case IList collection:
            {
              foreach (object obj in collection)
              {
                if (obj is Base b)
                  stack.Push(b);
              }
              break;
            }
          }
        }
      }
    }
    
    public static string ToFormattedString(this Exception exception)
    {
      var messages = exception
        .GetAllExceptions()
        .Where(e => !string.IsNullOrWhiteSpace(e.Message))
        .Select(e => e.Message.Trim());
      string flattened = string.Join(Environment.NewLine + "    ", messages); // <-- the separator here
      return flattened;
    }

    private static IEnumerable<Exception> GetAllExceptions(this Exception exception)
    {
      yield return exception;

      if (exception is AggregateException aggrEx)
      {
        foreach (var innerEx in aggrEx.InnerExceptions.SelectMany(e => e.GetAllExceptions()))
          yield return innerEx;
      }
      else if (exception.InnerException != null)
      {
        foreach (var innerEx in exception.InnerException.GetAllExceptions())
          yield return innerEx;
      }
    }
  }
}
