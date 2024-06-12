using System.DoubleNumerics;
using Rhino.DocObjects;
using Speckle.Connectors.Utils.Instances;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Rhino7.HostApp;

/// <summary>
/// <inheritdoc/>
/// </summary>
public class RhinoInstanceObjectsManager : IInstanceObjectsManager<RhinoObject>
{
  public RhinoInstanceObjectsManager()
  {
    // TODO: remove
  }

  private Dictionary<string, InstanceProxy> InstanceProxies { get; set; } = new();
  private Dictionary<string, List<InstanceProxy>> InstanceProxiesByDefinitionId { get; set; } = new();
  private Dictionary<string, InstanceDefinitionProxy> DefinitionProxies { get; set; } = new();
  private Dictionary<string, RhinoObject> FlatAtomicObjects { get; set; } = new();

  public UnpackResult<RhinoObject> UnpackSelection(IEnumerable<RhinoObject> objects)
  {
    foreach (var obj in objects)
    {
      if (obj is InstanceObject instanceObject)
      {
        UnpackInstance(instanceObject);
      }
      FlatAtomicObjects[obj.Id.ToString()] = obj;
    }
    return new(FlatAtomicObjects.Values.ToList(), InstanceProxies, DefinitionProxies.Values.ToList());
  }

  private void UnpackInstance(InstanceObject instance, int depth = 0)
  {
    var instanceId = instance.Id.ToString();
    var instanceDefinitionId = instance.InstanceDefinition.Id.ToString();
    InstanceProxies[instanceId] = new InstanceProxy()
    {
      applicationId = instanceId,
      DefinitionId = instance.InstanceDefinition.Id.ToString(),
      Transform = XFormToMatrix(instance.InstanceXform),
      MaxDepth = depth
    };

    // For each block instance that has the same definition, we need to keep track of the "maximum depth" at which is found.
    // This will enable on receive to create them in the correct order (descending by max depth, interleaved definitions and instances).
    // We need to interleave the creation of definitions and instances, as some definitions may depend on instances.
    if (
      !InstanceProxiesByDefinitionId.TryGetValue(
        instanceDefinitionId,
        out List<InstanceProxy> instanceProxiesWithSameDefinition
      )
    )
    {
      instanceProxiesWithSameDefinition = new List<InstanceProxy>();
      InstanceProxiesByDefinitionId[instanceDefinitionId] = instanceProxiesWithSameDefinition;
    }

    // We ensure that all previous instance proxies that have the same definition are at this max depth. I kind of have a feeling this can be done more elegantly, but YOLO
    foreach (var instanceProxy in instanceProxiesWithSameDefinition)
    {
      instanceProxy.MaxDepth = depth;
    }

    instanceProxiesWithSameDefinition.Add(InstanceProxies[instanceId]);

    if (DefinitionProxies.TryGetValue(instanceDefinitionId, out InstanceDefinitionProxy value))
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

    DefinitionProxies[instance.InstanceDefinition.Id.ToString()] = definition;

    foreach (var obj in instance.InstanceDefinition.GetObjects())
    {
      definition.Objects.Add(obj.Id.ToString());
      if (obj is InstanceObject localInstance)
      {
        UnpackInstance(localInstance, depth + 1);
      }
      FlatAtomicObjects[obj.Id.ToString()] = obj;
    }
  }

  private Matrix4x4 XFormToMatrix(Rhino.Geometry.Transform t) =>
    new(t.M00, t.M01, t.M02, t.M03, t.M10, t.M11, t.M12, t.M13, t.M20, t.M21, t.M22, t.M23, t.M30, t.M31, t.M32, t.M33);
}
