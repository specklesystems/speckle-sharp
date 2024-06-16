using System.DoubleNumerics;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Connectors.Rhino7.Extensions;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Rhino7.HostApp;

/// <summary>
/// <inheritdoc/>
///  Expects to be a scoped dependency per send or receive operation.
/// </summary>
public class RhinoInstanceObjectsManager : IInstanceObjectsManager<RhinoObject>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly Dictionary<string, InstanceProxy> _instanceProxies = new();
  private readonly Dictionary<string, List<InstanceProxy>> _instanceProxiesByDefinitionId = new();
  private readonly Dictionary<string, InstanceDefinitionProxy> _definitionProxies = new();
  private readonly Dictionary<string, RhinoObject> _flatAtomicObjects = new();
  private readonly RhinoLayerManager _layerManager;

  public RhinoInstanceObjectsManager(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    RhinoLayerManager layerManager
  )
  {
    _contextStack = contextStack;
    _layerManager = layerManager;
  }

  public UnpackResult<RhinoObject> UnpackSelection(IEnumerable<RhinoObject> objects)
  {
    foreach (var obj in objects)
    {
      if (obj is InstanceObject instanceObject)
      {
        UnpackInstance(instanceObject);
      }
      _flatAtomicObjects[obj.Id.ToString()] = obj;
    }
    return new(_flatAtomicObjects.Values.ToList(), _instanceProxies, _definitionProxies.Values.ToList());
  }

  private void UnpackInstance(InstanceObject instance, int depth = 0)
  {
    var instanceId = instance.Id.ToString();
    var instanceDefinitionId = instance.InstanceDefinition.Id.ToString();

    _instanceProxies[instanceId] = new InstanceProxy()
    {
      applicationId = instanceId,
      DefinitionId = instance.InstanceDefinition.Id.ToString(),
      Transform = XFormToMatrix(instance.InstanceXform),
      MaxDepth = depth,
      Units = _contextStack.Current.Document.ModelUnitSystem.ToSpeckleString()
    };

    // For each block instance that has the same definition, we need to keep track of the "maximum depth" at which is found.
    // This will enable on receive to create them in the correct order (descending by max depth, interleaved definitions and instances).
    // We need to interleave the creation of definitions and instances, as some definitions may depend on instances.
    if (
      !_instanceProxiesByDefinitionId.TryGetValue(
        instanceDefinitionId,
        out List<InstanceProxy> instanceProxiesWithSameDefinition
      )
    )
    {
      instanceProxiesWithSameDefinition = new List<InstanceProxy>();
      _instanceProxiesByDefinitionId[instanceDefinitionId] = instanceProxiesWithSameDefinition;
    }

    // We ensure that all previous instance proxies that have the same definition are at this max depth. I kind of have a feeling this can be done more elegantly, but YOLO
    foreach (var instanceProxy in instanceProxiesWithSameDefinition)
    {
      instanceProxy.MaxDepth = depth;
    }

    instanceProxiesWithSameDefinition.Add(_instanceProxies[instanceId]);

    if (_definitionProxies.TryGetValue(instanceDefinitionId, out InstanceDefinitionProxy value))
    {
      value.MaxDepth = depth;
      return;
    }

    var definition = new InstanceDefinitionProxy
    {
      applicationId = instanceDefinitionId,
      Objects = new List<string>(),
      MaxDepth = depth,
      ["name"] = instance.InstanceDefinition.Name,
      ["description"] = instance.InstanceDefinition.Description
    };

    _definitionProxies[instance.InstanceDefinition.Id.ToString()] = definition;

    foreach (var obj in instance.InstanceDefinition.GetObjects())
    {
      definition.Objects.Add(obj.Id.ToString());
      if (obj is InstanceObject localInstance)
      {
        UnpackInstance(localInstance, depth + 1);
      }
      _flatAtomicObjects[obj.Id.ToString()] = obj;
    }
  }

  /// <summary>
  /// Bakes in the host app doc instances. Assumes constituent atomic objects already present in the host app.
  /// </summary>
  /// <param name="instanceComponents">Instance definitions and instances that need creating.</param>
  /// <param name="applicationIdMap">A dict mapping { original application id -> [resulting application ids post conversion] }</param>
  /// <param name="onOperationProgressed"></param>
  public BakeResult BakeInstances(
    List<(string[] layerPath, IInstanceComponent obj)> instanceComponents,
    Dictionary<string, List<string>> applicationIdMap,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed
  )
  {
    var doc = _contextStack.Current.Document;
    var sortedInstanceComponents = instanceComponents
      .OrderByDescending(x => x.obj.MaxDepth) // Sort by max depth, so we start baking from the deepest element first
      .ThenBy(x => x.obj is InstanceDefinitionProxy ? 0 : 1) // Ensure we bake the deepest definition first, then any instances that depend on it
      .ToList();
    var definitionIdAndApplicationIdMap = new Dictionary<string, int>();

    var count = 0;
    var conversionResults = new List<ReceiveConversionResult>();
    var createdObjectIds = new List<string>();
    var consumedObjectIds = new List<string>();
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
          consumedObjectIds.AddRange(currentApplicationObjectsIds);
          createdObjectIds.RemoveAll(id => consumedObjectIds.Contains(id)); // in case we've consumed some existing instances
        }

        if (
          instanceOrDefinition is InstanceProxy instanceProxy
          && definitionIdAndApplicationIdMap.TryGetValue(instanceProxy.DefinitionId, out int index)
        )
        {
          var transform = MatrixToTransform(instanceProxy.Transform, instanceProxy.Units);
          var layerIndex = _layerManager.GetAndCreateLayerFromPath(path, baseLayerName);
          var id = doc.Objects.AddInstanceObject(index, transform, new ObjectAttributes() { LayerIndex = layerIndex });
          if (instanceProxy.applicationId != null)
          {
            applicationIdMap[instanceProxy.applicationId] = new List<string>() { id.ToString() };
          }

          createdObjectIds.Add(id.ToString());
          conversionResults.Add(new(Status.SUCCESS, instanceProxy, id.ToString(), "Instance (Block)"));
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        conversionResults.Add(new(Status.ERROR, instanceOrDefinition as Base ?? new Base(), null, null, ex));
      }
    }

    return new(createdObjectIds, consumedObjectIds, conversionResults);
  }

  private Matrix4x4 XFormToMatrix(Transform t) =>
    new(t.M00, t.M01, t.M02, t.M03, t.M10, t.M11, t.M12, t.M13, t.M20, t.M21, t.M22, t.M23, t.M30, t.M31, t.M32, t.M33);

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
