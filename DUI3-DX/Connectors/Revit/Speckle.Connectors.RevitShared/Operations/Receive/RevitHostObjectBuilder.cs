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

  //private IReadOnlyList<string> HandleConversionResult(object conversionResult, Base originalObject, int layerIndex)
  //{
  //  var doc = _contextStack.Current.Document;
  //  List<string> newObjectIds = new();
  //  switch (conversionResult)
  //  {
  //    case IEnumerable<IRhinoGeometryBase> list:
  //      {
  //        var group = BakeObjectsAsGroup(originalObject.id, list, layerIndex);
  //        newObjectIds.Add(group.Id.ToString());
  //        break;
  //      }
  //    case IRhinoGeometryBase newObject:
  //      {
  //        var newObjectGuid = doc.Objects.Add(newObject, _rhinoDocFactory.CreateAttributes(layerIndex));
  //        newObjectIds.Add(newObjectGuid.ToString());
  //        break;
  //      }
  //    default:
  //      throw new SpeckleConversionException(
  //        $"Unexpected result from conversion: Expected {nameof(IRhinoGeometryBase)} but instead got {conversionResult.GetType().Name}"
  //      );
  //  }

  //  return newObjectIds;
  //}

  //private IRhinoGroup BakeObjectsAsGroup(string groupName, IEnumerable<IRhinoGeometryBase> list, int layerIndex)
  //{
  //  var doc = _contextStack.Current.Document;
  //  var objectIds = list.Select(obj => doc.Objects.Add(obj, _rhinoDocFactory.CreateAttributes(layerIndex)));
  //  var groupIndex = _contextStack.Current.Document.Groups.Add(groupName, objectIds);
  //  var group = _contextStack.Current.Document.Groups.FindIndex(groupIndex);
  //  return group;
  //}

  //// POC: This is the original DUI3 function, this will grow over time as we add more conversions that are missing, so it should be refactored out into an ILayerManager or some sort of service.
  //private int GetAndCreateLayerFromPath(string[] path, string baseLayerName, Dictionary<string, int> cache)
  //{
  //  var currentLayerName = baseLayerName;
  //  var currentDocument = _contextStack.Current.Document;

  //  var previousLayer = currentDocument.Layers.FindName(currentLayerName);
  //  foreach (var layerName in path)
  //  {
  //    currentLayerName = baseLayerName + _rhinoDocFactory.LayerPathSeparator + layerName;
  //    currentLayerName = currentLayerName.Replace("{", "").Replace("}", ""); // Rhino specific cleanup for gh (see RemoveInvalidRhinoChars)
  //    if (cache.TryGetValue(currentLayerName, out int value))
  //    {
  //      previousLayer = currentDocument.Layers.FindIndex(value);
  //      continue;
  //    }

  //    var cleanNewLayerName = layerName.Replace("{", "").Replace("}", "");
  //    var newLayer = _rhinoDocFactory.CreateLayer(cleanNewLayerName, previousLayer.Id);
  //    var index = currentDocument.Layers.Add(newLayer);
  //    cache.Add(currentLayerName, index);
  //    previousLayer = currentDocument.Layers.FindIndex(index); // note we need to get the correct id out, hence why we're double calling this
  //  }
  //  return previousLayer.Index;
  //}

  //[Pure]
  //private static string[] GetLayerPath(TraversalContext context)
  //{
  //  string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
  //  string[] reverseOrderPath =
  //    collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();
  //  return reverseOrderPath.Reverse().ToArray();
  //}
}
