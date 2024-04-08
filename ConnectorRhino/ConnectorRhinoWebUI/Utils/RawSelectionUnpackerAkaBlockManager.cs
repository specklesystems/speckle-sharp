using System.Collections.Generic;
using System.DoubleNumerics;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Models;

namespace ConnectorRhinoWebUI.Utils;

/// <summary>
/// Unpacks a given list of rhino objects into their sub-constituent parts.
/// This mofo is ~ready to be interfaced out, pending the test of the acad implementation. Note that it's a POC class, and we're doing the following nasty things in here:
/// - transforms are converted ad hoc, can be fixed in the DX branch where there's proper DI and we can get in here a conversion routine for xform to other.transform
/// - InstanceProxy and InstanceDefinitionProxy types are really raw, and specified in probably the wrong place (Core, and should be in Objects)
/// </summary>
public class RawSelectionUnpackerAkaBlockManager // naming is hard
{
  private Dictionary<string, InstanceProxy> InstanceProxies { get; set; } = new();
  private Dictionary<string, List<InstanceProxy>> InstanceProxiesByDefinitionId { get; set; } = new();
  private Dictionary<string, InstanceDefinitionProxy> DefinitionProxies { get; set; } = new();
  private Dictionary<string, RhinoObject> FlatAtomicObjects { get; set; } = new();

  /// <summary>
  /// Unpacks a given list of objects into their atomic objects (raw geometry and instances) and any instance definitions.
  /// </summary>
  /// <param name="objects">Objects to unpack</param>
  /// <returns>A tuple (because we're lazy), consisting of atomic objects (raw geometry), a list of instance proxies (to enable swapping the actual instance to an instance proxy during the conversion loop), and a list of instance definition proxies (to attach separately to the root commit object).</returns>
  public (
    List<RhinoObject> atomicObjects,
    Dictionary<string, InstanceProxy> instanceProxies,
    List<InstanceDefinitionProxy> instanceDefinitionProxies
  ) Unpack(List<RhinoObject> objects)
  {
    InstanceProxies = new();
    DefinitionProxies = new();
    FlatAtomicObjects = new();
    InstanceProxiesByDefinitionId = new();

    foreach (var obj in objects)
    {
      if (obj is InstanceObject instanceObject)
      {
        UnpackInstance(instanceObject);
      }
      FlatAtomicObjects[obj.Id.ToString()] = obj;
    }

    return (FlatAtomicObjects.Values.ToList(), InstanceProxies, DefinitionProxies.Values.ToList());
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

  /// <summary>
  /// POC: For change detection "inside" instances
  /// TODO: Test & implement in the change detection part of the send binding
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public static List<string> UnpackObjectToRawIds(RhinoObject obj)
  {
    var list = new List<string>();
    void UnpackInstance(InstanceObject instance)
    {
      list.Add(instance.Id.ToString());
      foreach (var obj in instance.InstanceDefinition.GetObjects())
      {
        list.Add(obj.Id.ToString());
        if (obj is InstanceObject i)
        {
          UnpackInstance(i);
        }
      }
    }

    if (obj is InstanceObject instanceObject)
    {
      UnpackInstance(instanceObject);
    }
    else if (obj is not null)
    {
      list.Add(obj.Id.ToString());
    }
    return list;
  }

  public static List<string> UnpackObjectListToRawIds(IEnumerable<RhinoObject> objects)
  {
    var set = new HashSet<string>();
    foreach (var obj in objects)
    {
      set.UnionWith(UnpackObjectToRawIds(obj));
    }

    return set.ToList();
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
