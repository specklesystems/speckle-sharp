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

  // POC
  public static IEnumerable<RhinoObject> UnpackInstanceDefinition(InstanceDefinition definition)
  {
    var stack = new Stack<RhinoObject>();
    foreach (var obj in definition.GetObjects())
    {
      stack.Push(obj);
    }

    while (stack.Count > 0)
    {
      var obj = stack.Pop();
      if (obj is InstanceObject block)
      {
        foreach (var VARIABLE in block.InstanceDefinition.GetObjects())
        {
          stack.Push(VARIABLE);
        }
        continue;
      }

      yield return obj;
    }
  }
}

public class RhinoInstanceUnpacker
{
  public Dictionary<string, InstanceProxy> FlatBlocks { get; set; } = new();
  public Dictionary<string, InstanceDefinitionProxy> FlatDefinitions { get; set; } = new();
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
    if (FlatBlocks.ContainsKey(instance.Id.ToString()))
    {
      FlatBlocks[instance.Id.ToString()].MaxDepth = depth;
      return;
    }

    FlatBlocks[instance.Id.ToString()] = new InstanceProxy()
    {
      applicationId = instance.Id.ToString(),
      DefinitionId = instance.InstanceDefinition.Id.ToString(),
      Transform = XFormToMatrix(instance.InstanceXform),
      MaxDepth = depth
    };

    if (FlatDefinitions.ContainsKey(instance.InstanceDefinition.Id.ToString()))
    {
      FlatDefinitions[instance.InstanceDefinition.Id.ToString()].MaxDepth = depth;
      return;
    }

    var def = new InstanceDefinitionProxy
    {
      applicationId = instance.InstanceDefinition.Id.ToString(),
      Objects = new List<string>()
    };

    FlatDefinitions[instance.InstanceDefinition.Id.ToString()] = def;

    foreach (var obj in instance.InstanceDefinition.GetObjects())
    {
      def.Objects.Add(obj.Id.ToString());

      if (obj is InstanceObject localInstance)
      {
        UnpackInstance(localInstance, depth + 1);
      }
      else
      {
        FlatAtomicObjects[obj.Id.ToString()] = obj;
      }
    }
  }

  private Matrix4x4 XFormToMatrix(Transform t) =>
    new(t.M00, t.M01, t.M02, t.M03, t.M10, t.M11, t.M12, t.M13, t.M20, t.M21, t.M22, t.M23, t.M30, t.M31, t.M32, t.M33);
}

public class InstanceProxy : Base
{
  // public string Id { get; set; }
  public string DefinitionId { get; set; }
  public Matrix4x4 Transform { get; set; }
  public int MaxDepth { get; set; } = 0;
}

public class InstanceDefinitionProxy : Base
{
  // public string Id { get; set; }
  public List<string> Objects { get; set; }
  public int MaxDepth { get; set; } = 0;
}
