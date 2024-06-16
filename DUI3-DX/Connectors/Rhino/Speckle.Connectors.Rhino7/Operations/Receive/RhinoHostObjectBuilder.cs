using System.DoubleNumerics;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Connectors.Rhino7.Extensions;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Rhino7.Operations.Receive;

public class RhinoHostObjectBuilder : IHostObjectBuilder
{
  private readonly IRootToHostConverter _converter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly GraphTraversal _traverseFunction;
  private readonly IInstanceObjectsManager<RhinoObject> _instanceObjectsManager;
  private readonly RhinoLayerManager _layerManager;

  public RhinoHostObjectBuilder(
    IRootToHostConverter converter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    GraphTraversal traverseFunction,
    RhinoLayerManager layerManager,
    IInstanceObjectsManager<RhinoObject> instanceObjectsManager
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

    // POC: these are not captured by traversal, so we need to readd them here
    if (instanceDefinitionProxies != null && instanceDefinitionProxies.Count > 0)
    {
      var transformed = instanceDefinitionProxies.Select(proxy => (Array.Empty<string>(), proxy as IInstanceComponent));
      instanceComponents.AddRange(transformed);
    }

    var atomicObjects = new List<(string[] layerPath, Base obj)>();
    IEnumerable<TraversalContext> traversalContexts = objectsGraph as TraversalContext[] ?? objectsGraph.ToArray();

    foreach (TraversalContext tc in traversalContexts)
    {
      var path = _layerManager.GetLayerPath(tc);
      if (tc.Current is IInstanceComponent flocker)
      {
        instanceComponents.Add((path, flocker));
      }
      else
      {
        atomicObjects.Add((path, tc.Current));
      }
    }

    // Stage 1: Convert atomic objects
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
          // TODO: groups inside blocks? is that a thing? can we account for that? HOW CAN WE ACCOUNT FOR THAT?
          // ie, what happens when we receive a block that contains one object that we need to explode in host app?
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
    var doc = _contextStack.Current.Document;

    // Cleanup blocks/definitions/instances before layers
    foreach (var definition in doc.InstanceDefinitions)
    {
      if (!definition.IsDeleted && definition.Name.Contains(baseLayerName))
      {
        doc.InstanceDefinitions.Delete(definition.Index, true, false);
      }
    }

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

  // POC: Not too proud of this being here, will be moving soon to the instance manager
  private Transform MatrixToTransform(Matrix4x4 matrix, string units)
  {
    var conversionFactor = Units.GetConversionFactor(
      units,
      _contextStack.Current.Document.ModelUnitSystem.ToSpeckleString()
    );

    var t = Transform.Identity;
    t.M00 = matrix.M11;
    t.M01 = matrix.M12;
    t.M02 = matrix.M13;
    t.M03 = matrix.M14 * conversionFactor;

    t.M10 = matrix.M21;
    t.M11 = matrix.M22;
    t.M12 = matrix.M23;
    t.M13 = matrix.M24 * conversionFactor;

    t.M20 = matrix.M31;
    t.M21 = matrix.M32;
    t.M22 = matrix.M33;
    t.M23 = matrix.M34 * conversionFactor;

    t.M30 = matrix.M41;
    t.M31 = matrix.M42;
    t.M32 = matrix.M43;
    t.M33 = matrix.M44;
    return t;
  }
}
