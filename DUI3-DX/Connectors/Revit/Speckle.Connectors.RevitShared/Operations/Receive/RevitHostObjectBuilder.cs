using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Models;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Operations.Receive;

internal class RevitHostObjectBuilder : IHostObjectBuilder
{
  private readonly IRootToHostConverter _converter;
  private readonly IRevitConversionContextStack _contextStack;
  private readonly GraphTraversal _traverseFunction;
  private readonly TransactionManager _transactionManager;

  public RevitHostObjectBuilder(
    IRootToHostConverter converter,
    IRevitConversionContextStack contextStack,
    GraphTraversal traverseFunction,
    TransactionManager transactionManager
  )
  {
    _converter = converter;
    _contextStack = contextStack;
    _traverseFunction = traverseFunction;
    _transactionManager = transactionManager;
  }

  public HostObjectBuilderResult Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    var objectsToConvert = _traverseFunction
      .TraverseWithProgress(rootObject, onOperationProgressed, cancellationToken)
      .Where(obj => obj.Current is not Collection);

    _transactionManager.StartTransactionGroup($"Received data from {projectName}");

    var conversionResults = BakeObjects(objectsToConvert);

    _transactionManager.CommitTransactionGroup();
    _transactionManager.Dispose();

    return conversionResults;
  }

  // POC: Potentially refactor out into an IObjectBaker.
  private HostObjectBuilderResult BakeObjects(IEnumerable<TraversalContext> objectsGraph)
  {
    var conversionResults = new List<ReceiveConversionResult>();
    var bakedObjectIds = new List<string>();

    foreach (TraversalContext tc in objectsGraph)
    {
      try
      {
        YieldToUiThread();
        var result = _converter.Convert(tc.Current);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        conversionResults.Add(new(Status.ERROR, tc.Current, null, null, ex));
      }
    }

    return new(bakedObjectIds, conversionResults);
  }

  private DateTime _timerStarted = DateTime.MinValue;

  private void YieldToUiThread()
  {
    var currentTime = DateTime.UtcNow;

    if (currentTime.Subtract(_timerStarted) < TimeSpan.FromSeconds(.15))
    {
      return;
    }

    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
      () => { },
      System.Windows.Threading.DispatcherPriority.Background
    );

    _timerStarted = currentTime;
  }
}
