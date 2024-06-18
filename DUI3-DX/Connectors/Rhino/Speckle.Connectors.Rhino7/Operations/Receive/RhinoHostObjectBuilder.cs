using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Rhino7.Operations.Receive;

/// <summary>
/// <para>Expects to be a scoped dependency per receive operation.</para>
/// </summary>
public class RhinoHostObjectBuilder : IHostObjectBuilder
{
  private readonly IRootToHostConverter _converter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly GraphTraversal _traverseFunction;
  private readonly IInstanceObjectsManager<RhinoObject, List<string>> _instanceObjectsManager;
  private readonly RhinoLayerManager _layerManager;

  public RhinoHostObjectBuilder(
    IRootToHostConverter converter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    GraphTraversal traverseFunction,
    RhinoLayerManager layerManager,
    IInstanceObjectsManager<RhinoObject, List<string>> instanceObjectsManager
  )
  {
    _converter = converter;
    _contextStack = contextStack;
    _traverseFunction = traverseFunction;
    _layerManager = layerManager;
    _instanceObjectsManager = instanceObjectsManager;
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

    var instanceDefinitionProxies = (rootObject["instanceDefinitionProxies"] as List<object>)
      ?.Cast<InstanceDefinitionProxy>()
      .ToList();

    var conversionResults = BakeObjects(
      objectsToConvert,
      instanceDefinitionProxies,
      baseLayerName,
      onOperationProgressed
    );

    _contextStack.Current.Document.Views.Redraw();

    return conversionResults;
  }

  private HostObjectBuilderResult BakeObjects(
    IEnumerable<TraversalContext> objectsGraph,
    List<InstanceDefinitionProxy>? instanceDefinitionProxies,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed
  )
  {
    RhinoDoc doc = _contextStack.Current.Document;

    var rootLayerIndex = _contextStack.Current.Document.Layers.Find(Guid.Empty, baseLayerName, RhinoMath.UnsetIntIndex);
    PreReceiveDeepClean(baseLayerName, rootLayerIndex);
    _layerManager.CreateBaseLayer(baseLayerName);

    using var noDraw = new DisableRedrawScope(doc.Views);

    var conversionResults = new List<ReceiveConversionResult>();
    var bakedObjectIds = new List<string>();

    var instanceComponents = new List<(string[] layerPath, IInstanceComponent obj)>();

    // POC: these are not captured by traversal, so we need to re-add them here
    if (instanceDefinitionProxies != null && instanceDefinitionProxies.Count > 0)
    {
      var transformed = instanceDefinitionProxies.Select(proxy => (Array.Empty<string>(), proxy as IInstanceComponent));
      instanceComponents.AddRange(transformed);
    }

    var atomicObjects = new List<(string[] layerPath, Base obj)>();

    // Split up the instances from the non-instances
    foreach (TraversalContext tc in objectsGraph)
    {
      var path = _layerManager.GetLayerPath(tc);
      if (tc.Current is IInstanceComponent instanceComponent)
      {
        instanceComponents.Add((path, instanceComponent));
      }
      else
      {
        atomicObjects.Add((path, tc.Current));
      }
    }

    // Stage 1: Convert atomic objects
    // Note: this can become encapsulated later in an "atomic object baker" of sorts, if needed.
    var applicationIdMap = new Dictionary<string, List<string>>(); // used in converting blocks in stage 2. keeps track of original app id => resulting new app ids post baking
    var count = 0;
    foreach (var (path, obj) in atomicObjects)
    {
      onOperationProgressed?.Invoke("Converting objects", (double)++count / atomicObjects.Count);
      try
      {
        var layerIndex = _layerManager.GetAndCreateLayerFromPath(path, baseLayerName);
        var result = _converter.Convert(obj);
        var conversionIds = HandleConversionResult(result, obj, layerIndex).ToList();
        foreach (var r in conversionIds)
        {
          conversionResults.Add(new(Status.SUCCESS, obj, r, result.GetType().ToString()));
          bakedObjectIds.Add(r);
        }

        if (obj.applicationId != null)
        {
          applicationIdMap[obj.applicationId] = conversionIds;
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        conversionResults.Add(new(Status.ERROR, obj, null, null, ex));
      }
    }

    // Stage 2: Convert instances
    var (createdInstanceIds, consumedObjectIds, instanceConversionResults) = _instanceObjectsManager.BakeInstances(
      instanceComponents,
      applicationIdMap,
      baseLayerName,
      onOperationProgressed
    );

    bakedObjectIds.RemoveAll(id => consumedObjectIds.Contains(id)); // remove all objects that have been "consumed"
    bakedObjectIds.AddRange(createdInstanceIds); // add instance ids
    conversionResults.RemoveAll(result => result.ResultId != null && consumedObjectIds.Contains(result.ResultId)); // remove all conversion results for atomic objects that have been consumed (POC: not that cool, but prevents problems on object highlighting)
    conversionResults.AddRange(instanceConversionResults); // add instance conversion results to our list

    // Stage 3: Return
    return new(bakedObjectIds, conversionResults);
  }

  private void PreReceiveDeepClean(string baseLayerName, int rootLayerIndex)
  {
    _instanceObjectsManager.PurgeInstances(baseLayerName);

    var doc = _contextStack.Current.Document;
    // Cleans up any previously received objects
    if (rootLayerIndex != RhinoMath.UnsetIntIndex)
    {
      Layer documentLayer = doc.Layers[rootLayerIndex];
      Layer[]? childLayers = documentLayer.GetChildren();
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
  }

  private IReadOnlyList<string> HandleConversionResult(object conversionResult, Base originalObject, int layerIndex)
  {
    var doc = _contextStack.Current.Document;
    List<string> newObjectIds = new();
    switch (conversionResult)
    {
      case IEnumerable<GeometryBase> list:
      {
        Group group = BakeObjectsAsGroup(originalObject.id, list, layerIndex);
        newObjectIds.Add(group.Id.ToString());
        break;
      }
      case GeometryBase newObject:
      {
        var newObjectGuid = doc.Objects.Add(newObject, new ObjectAttributes { LayerIndex = layerIndex });
        newObjectIds.Add(newObjectGuid.ToString());
        break;
      }
      default:
        throw new SpeckleConversionException(
          $"Unexpected result from conversion: Expected {nameof(GeometryBase)} but instead got {conversionResult.GetType().Name}"
        );
    }

    return newObjectIds;
  }

  private Group BakeObjectsAsGroup(string groupName, IEnumerable<GeometryBase> list, int layerIndex)
  {
    var doc = _contextStack.Current.Document;
    var objectIds = list.Select(obj => doc.Objects.Add(obj, new ObjectAttributes { LayerIndex = layerIndex }));
    var groupIndex = _contextStack.Current.Document.Groups.Add(groupName, objectIds);
    var group = _contextStack.Current.Document.Groups.FindIndex(groupIndex);
    return group;
  }
}
