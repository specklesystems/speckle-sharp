using System.Diagnostics.Contracts;
using System.DoubleNumerics;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
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

  public RhinoHostObjectBuilder(
    IRootToHostConverter converter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    GraphTraversal traverseFunction,
    IInstanceObjectsManager<RhinoObject> instanceObjectsManager
  )
  {
    _converter = converter;
    _contextStack = contextStack;
    _traverseFunction = traverseFunction;
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

  // POC: Potentially refactor out into an IObjectBaker.
  // POC: temp disable
#pragma warning disable CA1506
#pragma warning disable CA1502
  private HostObjectBuilderResult BakeObjects(
#pragma warning restore CA1502
#pragma warning restore CA1506
    IEnumerable<TraversalContext> objectsGraph,
    List<InstanceDefinitionProxy>? instanceDefinitionProxies,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed
  )
  {
    RhinoDoc doc = _contextStack.Current.Document;
    var rootLayerIndex = _contextStack.Current.Document.Layers.Find(Guid.Empty, baseLayerName, RhinoMath.UnsetIntIndex);

    PreReceiveDeepClean(baseLayerName, rootLayerIndex);

    var cache = new Dictionary<string, int>();
    rootLayerIndex = doc.Layers.Add(new Layer { Name = baseLayerName });
    cache.Add(baseLayerName, rootLayerIndex);

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
      var path = GetLayerPath(tc);
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
    var applicationIdMap = new Dictionary<string, List<string>>(); // used in converting blocks in stage 2
    var count = 0;
    foreach (var (path, obj) in atomicObjects)
    {
      onOperationProgressed?.Invoke("Converting objects", (double)++count / atomicObjects.Count);
      try
      {
        var fullLayerName = string.Join(Layer.PathSeparator, path);
        var layerIndex = cache.TryGetValue(fullLayerName, out int value)
          ? value
          : GetAndCreateLayerFromPath(path, baseLayerName, cache);

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
    // TODO: do not forget to add to report things
    var sortedInstanceComponents = instanceComponents
      .OrderByDescending(x => x.obj.MaxDepth) // Sort by max depth, so we start baking from the deepest element first
      .ThenBy(x => x.obj is InstanceDefinitionProxy ? 0 : 1) // Ensure we bake the deepest definition first, then any instances that depend on it
      .ToList();
    var definitionIdAndApplicationIdMap = new Dictionary<string, int>();

    count = 0;
    foreach (var (path, instanceOrDefinition) in sortedInstanceComponents)
    {
      onOperationProgressed?.Invoke("Converting blocks", (double)++count / sortedInstanceComponents.Count);
      try
      {
        if (instanceOrDefinition is InstanceDefinitionProxy definitionProxy)
        {
          var currentApplicationObjectsIds = definitionProxy.Objects
            .Select(x => applicationIdMap.TryGetValue(x, out List<string> value) ? value : null)
            .Where(x => x is not null)
            .SelectMany(id => id)
            .ToList();

          var definitionGeometryList = new List<GeometryBase>();
          var attributes = new List<ObjectAttributes>();

          foreach (var id in currentApplicationObjectsIds)
          {
            var docObject = doc.Objects.FindId(new Guid(id));
            definitionGeometryList.Add(docObject.Geometry);
            attributes.Add(docObject.Attributes);
          }

          // POC: Currently we're relying on the definition name for identification if it's coming from speckle and from which model; could we do something else?
          var defName = $"{baseLayerName} ({definitionProxy.applicationId})";
          var defIndex = doc.InstanceDefinitions.Add(
            defName,
            "No description", // POC: perhaps bring it along from source? We'd need to look at ACAD first
            Point3d.Origin,
            definitionGeometryList,
            attributes
          );

          // POC: check on defIndex -1, means we haven't created anything - this is most likely an recoverable error at this stage
          if (defIndex == -1)
          {
            throw new ConversionException("Failed to create an instance defintion object.");
          }

          if (definitionProxy.applicationId != null)
          {
            definitionIdAndApplicationIdMap[definitionProxy.applicationId] = defIndex;
          }

          // Rhino deletes original objects on block creation - we should do the same.
          doc.Objects.Delete(currentApplicationObjectsIds.Select(stringId => new Guid(stringId)), false);
          bakedObjectIds.RemoveAll(id => currentApplicationObjectsIds.Contains(id));
          conversionResults.RemoveAll(
            conversionResult =>
              conversionResult.ResultId != null && currentApplicationObjectsIds.Contains(conversionResult.ResultId) // note: as in rhino created objects are deleted, highlighting them won't work
          );
        }

        if (
          instanceOrDefinition is InstanceProxy instanceProxy
          && definitionIdAndApplicationIdMap.TryGetValue(instanceProxy.DefinitionId, out int index)
        )
        {
          var transform = MatrixToTransform(instanceProxy.Transform);
          var layerIndex = GetAndCreateLayerFromPath(path, baseLayerName, cache);
          var id = doc.Objects.AddInstanceObject(index, transform, new ObjectAttributes() { LayerIndex = layerIndex });
          if (instanceProxy.applicationId != null)
          {
            applicationIdMap[instanceProxy.applicationId] = new List<string>() { id.ToString() };
          }

          bakedObjectIds.Add(id.ToString());
          conversionResults.Add(new(Status.SUCCESS, instanceProxy, id.ToString(), "Instance (Block)"));
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        conversionResults.Add(new(Status.ERROR, instanceOrDefinition as Base ?? new Base(), null, null, ex));
      }
    }

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

  // POC: This is the original DUI3 function, this will grow over time as we add more conversions that are missing, so it should be refactored out into an ILayerManager or some sort of service.
  private int GetAndCreateLayerFromPath(string[] path, string baseLayerName, Dictionary<string, int> cache)
  {
    var currentLayerName = baseLayerName;
    RhinoDoc currentDocument = _contextStack.Current.Document;

    var previousLayer = currentDocument.Layers.FindName(currentLayerName);
    foreach (var layerName in path)
    {
      currentLayerName = baseLayerName + Layer.PathSeparator + layerName;
      currentLayerName = currentLayerName.Replace("{", "").Replace("}", ""); // Rhino specific cleanup for gh (see RemoveInvalidRhinoChars)
      if (cache.TryGetValue(currentLayerName, out int value))
      {
        previousLayer = currentDocument.Layers.FindIndex(value);
        continue;
      }

      var cleanNewLayerName = layerName.Replace("{", "").Replace("}", "");
      var newLayer = new Layer { Name = cleanNewLayerName, ParentLayerId = previousLayer.Id };
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

  // POC: Not too proud of this being here
  private Rhino.Geometry.Transform MatrixToTransform(Matrix4x4 matrix)
  {
    var t = Rhino.Geometry.Transform.Identity;
    t.M00 = matrix.M11;
    t.M01 = matrix.M12;
    t.M02 = matrix.M13;
    t.M03 = matrix.M14;

    t.M10 = matrix.M21;
    t.M11 = matrix.M22;
    t.M12 = matrix.M23;
    t.M13 = matrix.M24;

    t.M20 = matrix.M31;
    t.M21 = matrix.M32;
    t.M22 = matrix.M33;
    t.M23 = matrix.M34;

    t.M30 = matrix.M41;
    t.M31 = matrix.M42;
    t.M32 = matrix.M43;
    t.M33 = matrix.M44;
    return t;
  }
}
