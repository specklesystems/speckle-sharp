using System.Diagnostics.Contracts;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Connectors.Rhino7.Operations.Receive;

public class RhinoHostObjectBuilder : IHostObjectBuilder
{
  private readonly IRootToHostConverter _converter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly GraphTraversal _traverseFunction;
  private readonly IRhinoDocFactory _rhinoDocFactory;

  public RhinoHostObjectBuilder(
    IRootToHostConverter converter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    GraphTraversal traverseFunction, IRhinoDocFactory rhinoDocFactory)
  {
    _converter = converter;
    _contextStack = contextStack;
    _traverseFunction = traverseFunction;
    _rhinoDocFactory = rhinoDocFactory;
  }

  public HostObjectBuilderResult Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // POC: This is where the top level base-layer name is set. Could be abstracted or injected in the context?
    var baseLayerName = $"Project {projectName}: Model {modelName}";

    var objectsToConvert = _traverseFunction
      .TraverseWithProgress(rootObject, onOperationProgressed, cancellationToken)
      .Where(obj => obj.Current is not Collection);

    var conversionResults = BakeObjects(objectsToConvert, baseLayerName);

    _contextStack.Current.Document.Views.Redraw();

    return conversionResults;
  }

  // POC: Potentially refactor out into an IObjectBaker.
  private HostObjectBuilderResult BakeObjects(IEnumerable<TraversalContext> objectsGraph, string baseLayerName)
  {
    var doc = _contextStack.Current.Document;
    var rootLayerIndex = _contextStack.Current.Document.Layers.Find(Guid.Empty, baseLayerName, _rhinoDocFactory.UnsetIntIndex);

    // POC: We could move this out into a separate service for testing and re-use.
    // Cleans up any previously received objects
    if (rootLayerIndex != _rhinoDocFactory.UnsetIntIndex)
    {
      var documentLayer = doc.Layers[rootLayerIndex];
      var childLayers = documentLayer.GetChildren();
      if (childLayers != null)
      {
        using var layerNoDraw = new DisableRedrawScope(doc.Views);
        foreach (var layer in childLayers)
        {
          var purgeSuccess = doc.Layers.Purge(layer.Index, true);
          if (!purgeSuccess)
          {
            Console.WriteLine($"Failed to purge layer: {layer}");
          }
        }
      }
    }

    var cache = new Dictionary<string, int>();
    rootLayerIndex = doc.Layers.Add(_rhinoDocFactory.CreateLayer(baseLayerName));
    cache.Add(baseLayerName, rootLayerIndex);

    using var noDraw = new DisableRedrawScope(doc.Views);

    var conversionResults = new List<ReceiveConversionResult>();
    var bakedObjectIds = new List<string>();

    foreach (TraversalContext tc in objectsGraph)
    {
      try
      {
        var path = GetLayerPath(tc);

        var fullLayerName = string.Join(_rhinoDocFactory.LayerPathSeparator, path);
        var layerIndex = cache.TryGetValue(fullLayerName, out int value)
          ? value
          : GetAndCreateLayerFromPath(path, baseLayerName, cache);

        var result = _converter.Convert(tc.Current);

        var conversionIds = HandleConversionResult(result, tc.Current, layerIndex);
        foreach (var r in conversionIds)
        {
          conversionResults.Add(new(Status.SUCCESS, tc.Current, r, result.GetType().ToString()));
          bakedObjectIds.Add(r);
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        conversionResults.Add(new(Status.ERROR, tc.Current, null, null, ex));
      }
    }

    return new(bakedObjectIds, conversionResults);
  }

  private IReadOnlyList<string> HandleConversionResult(object conversionResult, Base originalObject, int layerIndex)
  {
    var doc = _contextStack.Current.Document;
    List<string> newObjectIds = new();
    switch (conversionResult)
    {
      case IEnumerable<IRhinoGeometryBase> list:
      {
        var group = BakeObjectsAsGroup(originalObject.id, list, layerIndex);
        newObjectIds.Add(group.Id.ToString());
        break;
      }
      case IRhinoGeometryBase newObject:
      {
        var newObjectGuid = doc.Objects.Add(newObject, _rhinoDocFactory.CreateAttributes(layerIndex));
        newObjectIds.Add(newObjectGuid.ToString());
        break;
      }
      default:
        throw new SpeckleConversionException(
          $"Unexpected result from conversion: Expected {nameof(IRhinoGeometryBase)} but instead got {conversionResult.GetType().Name}"
        );
    }

    return newObjectIds;
  }

  private IRhinoGroup BakeObjectsAsGroup(string groupName, IEnumerable<IRhinoGeometryBase> list, int layerIndex)
  {
    var doc = _contextStack.Current.Document;
    var objectIds = list.Select(obj => doc.Objects.Add(obj,_rhinoDocFactory.CreateAttributes(layerIndex)));
    var groupIndex = _contextStack.Current.Document.Groups.Add(groupName, objectIds);
    var group = _contextStack.Current.Document.Groups.FindIndex(groupIndex);
    return group;
  }

  // POC: This is the original DUI3 function, this will grow over time as we add more conversions that are missing, so it should be refactored out into an ILayerManager or some sort of service.
  private int GetAndCreateLayerFromPath(string[] path, string baseLayerName, Dictionary<string, int> cache)
  {
    var currentLayerName = baseLayerName;
    var currentDocument = _contextStack.Current.Document;

    var previousLayer = currentDocument.Layers.FindName(currentLayerName);
    foreach (var layerName in path)
    {
      currentLayerName = baseLayerName + _rhinoDocFactory.LayerPathSeparator + layerName;
      currentLayerName = currentLayerName.Replace("{", "").Replace("}", ""); // Rhino specific cleanup for gh (see RemoveInvalidRhinoChars)
      if (cache.TryGetValue(currentLayerName, out int value))
      {
        previousLayer = currentDocument.Layers.FindIndex(value);
        continue;
      }

      var cleanNewLayerName = layerName.Replace("{", "").Replace("}", "");
      var newLayer = _rhinoDocFactory.CreateLayer(cleanNewLayerName, previousLayer.Id);
      var index = currentDocument.Layers.Add(newLayer);
      cache.Add(currentLayerName, index);
      previousLayer = currentDocument.Layers.FindIndex(index); // note we need to get the correct id out, hence why we're double calling this
    }
    return previousLayer.Index;
  }

  [Pure]
  private static string[] GetLayerPath(TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath =
      collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();
    return reverseOrderPath.Reverse().ToArray();
  }
}
