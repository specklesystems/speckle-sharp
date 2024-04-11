using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Models.GraphTraversal;

namespace DUI3.Utils;

public static class Traversal
{
  public static List<Base> GetObjectsToConvert(Base commitObject, ISpeckleConverter converter)
  {
    var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

    return traverseFunction
      .Traverse(commitObject)
      .Select(tc => tc.Current) // Previously we were creating ApplicationObject, now just returning Base object.
      .Reverse()
      .ToList();
  }

  /// <summary>
  /// A variation of the OG Traversal extension from Alan, but with tracking the object path as well.
  /// </summary>
  /// <param name="root"></param>
  /// <param name="recursionBreaker"></param>
  /// <returns></returns>
  public static IEnumerable<(List<string>, Base)> TraverseWithPath(
    this Base root,
    BaseExtensions.BaseRecursionBreaker recursionBreaker
  )
  {
    var stack = new Stack<(List<string>, Base)>();
    stack.Push((new List<string>(), root));

    while (stack.Count > 0)
    {
      (List<string> path, Base current) = stack.Pop();
      yield return (path, current);

      if (recursionBreaker(current))
      {
        continue;
      }

      foreach (string child in current.GetDynamicMemberNames())
      {
        // NOTE: we can store collections rather than just path names. Where we have an actual collection, use that, where not, create a mock one based on the prop name
        var localPathFragment = child;
        if (current is Collection { name: { } } c)
        {
          localPathFragment = c.name;
        }

        var newPath = new List<string>(path) { localPathFragment };
        switch (current[child])
        {
          case Base o:
            stack.Push((newPath, o));
            break;
          case IDictionary dictionary:
          {
            foreach (object obj in dictionary.Keys)
            {
              if (obj is Base b)
              {
                stack.Push((newPath, b));
              }
            }

            break;
          }
          case IList collection:
          {
            foreach (object obj in collection)
            {
              if (obj is Base b)
              {
                stack.Push((newPath, b));
              }
            }
            break;
          }
        }
      }
    }
  }

  /// <summary>
  /// Utility function to flatten a conversion result that might have nested lists of objects.
  /// This happens, for example, in the case of multiple display value fallbacks for a given object.
  /// </summary>
  /// <param name="item"></param>
  /// <returns></returns>
  public static List<object> FlattenToNativeConversionResult(object item)
  {
    var convertedList = new List<object>();
    void Flatten(object item)
    {
      if (item is IList list)
      {
        foreach (object child in list)
        {
          Flatten(child);
        }
      }
      else
      {
        convertedList.Add(item);
      }
    }
    Flatten(item);
    return convertedList;
  }
}
