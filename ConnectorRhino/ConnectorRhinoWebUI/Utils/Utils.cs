using System.Collections.Generic;
using System.DoubleNumerics;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace ConnectorRhinoWebUI.Utils;

public static class Utils
{
#if RHINO6
  public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v6);
  public static string AppName = "Rhino";
#elif RHINO7
    public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
    public static string AppName = "Rhino";
#else
  public static readonly string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
  public static readonly string AppName = "Rhino";
#endif
}

/// <summary>
/// POC: hacking blocks
/// </summary>
public class RhinoInstanceUnpacker
{
  public Dictionary<string, InstanceProxy> InstanceProxies { get; set; } = new();

  private readonly Dictionary<string, List<InstanceProxy>> _instanceProxiesByDefinitionId = new();
  public Dictionary<string, InstanceDefinitionProxy> DefinitionProxies { get; set; } = new();
  public Dictionary<string, RhinoObject> FlatAtomicObjects { get; set; } = new();

  public void Unpack(List<RhinoObject> objects)
  {
    foreach (var obj in objects)
    {
      if (obj is InstanceObject instanceObject)
      {
        UnpackInstance(instanceObject);
      }
      FlatAtomicObjects[obj.Id.ToString()] = obj;
    }
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
      !_instanceProxiesByDefinitionId.TryGetValue(
        instanceDefinitionId,
        out List<InstanceProxy> instanceProxiesWithSameDefinition
      )
    )
    {
      instanceProxiesWithSameDefinition = new List<InstanceProxy>();
      _instanceProxiesByDefinitionId[instanceDefinitionId] = instanceProxiesWithSameDefinition;
    }

    // We ensure that all previous instance proxies that have the same definition are at this max depth
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

  // POC: Shouldn't be here, should be in converters? Esp. re unit conversion/scaling factors?
  public static Matrix4x4 XFormToMatrix(Transform t) =>
    new(t.M00, t.M01, t.M02, t.M03, t.M10, t.M11, t.M12, t.M13, t.M20, t.M21, t.M22, t.M23, t.M30, t.M31, t.M32, t.M33);

  // POC: Ditto above comment
  public static Transform MatrixToTransform(Matrix4x4 matrix)
  {
    Transform t = Transform.Identity;
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
