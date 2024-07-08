using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Utils.Builders;

public static class TraversalExtensions
{
  public static IEnumerable<TraversalContext> TraverseWithProgress(
    this GraphTraversal traversalFunction,
    Base rootObject,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken = default
  )
  {
    var traversalGraph = traversalFunction.Traverse(rootObject).ToArray();
    int count = 0;
    foreach (var tc in traversalGraph)
    {
      cancellationToken.ThrowIfCancellationRequested();

      yield return tc;

      onOperationProgressed?.Invoke("Converting", (double)++count / traversalGraph.Length);
    }
  }
}
