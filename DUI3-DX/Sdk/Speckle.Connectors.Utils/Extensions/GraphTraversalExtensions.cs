using System.Linq;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Utils.Extensions;

public static class GraphTraversalExtensions
{
  public static string[] GetCurrentObjectPath(this TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath = collectionBasedPath.Any() ? collectionBasedPath : context.GetPropertyPath().ToArray();
    return reverseOrderPath.Reverse().ToArray();
  }
}
