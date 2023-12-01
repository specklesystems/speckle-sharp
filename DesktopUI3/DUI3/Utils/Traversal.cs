using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace DUI3.Utils;

public static class Traversal
{
  public static List<Base> GetObjectsToConvert(Base commitObject, ISpeckleConverter converter)
  {
    var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

    return traverseFunction
      .Traverse(commitObject)
      .Select(tc => tc.current) // Previously we were creating ApplicationObject, now just returning Base object.
      .Reverse()
      .ToList();
  }
}
