using System;
using System.Drawing;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Extras;

public class SpeckleStreamParam : GH_Param<GH_SpeckleStream>
{
  public SpeckleStreamParam()
    : base(
      "Speckle Stream",
      "SS",
      "A speckle data stream object.",
      ComponentCategories.PRIMARY_RIBBON,
      "Params",
      GH_ParamAccess.item
    ) { }

  public SpeckleStreamParam(IGH_InstanceDescription tag)
    : base(tag) { }

  public SpeckleStreamParam(IGH_InstanceDescription tag, GH_ParamAccess access)
    : base(tag, access) { }

  public SpeckleStreamParam(
    string name,
    string nickname,
    string description,
    string category,
    string subcategory,
    GH_ParamAccess access
  )
    : base(name, nickname, description, category, subcategory, access) { }

  public SpeckleStreamParam(string name, string nickname, string description, GH_ParamAccess access)
    : base(name, nickname, description, ComponentCategories.PRIMARY_RIBBON, "Params", access) { }

  protected override Bitmap Icon => Resources.StreamParam;
  public override GH_Exposure Exposure => GH_Exposure.hidden;
  public override Guid ComponentGuid => new("FB436A31-1CE9-413C-B524-8A574C0F842D");
}

public sealed class GH_SpeckleStream : GH_Goo<StreamWrapper>
{
  public GH_SpeckleStream()
  {
    Value = null;
  }

  public GH_SpeckleStream(GH_Goo<StreamWrapper> other)
    : base(other)
  {
    Value = other.Value;
  }

  public GH_SpeckleStream(StreamWrapper internal_data)
    : base(internal_data)
  {
    Value = internal_data;
  }

  public override StreamWrapper Value { get; set; }

  public override bool IsValid => Value != null;
  public override string TypeName => "Stream";
  public override string TypeDescription => "A speckle data stream";

  public static implicit operator StreamWrapper(GH_SpeckleStream d)
  {
    return d.Value;
  }

  public override IGH_Goo Duplicate()
  {
    return new GH_SpeckleStream(Value);
  }

  public override string ToString()
  {
    return Value != null ? Value.ToString() : "Empty speckle stream";
  }

  public override bool CastFrom(object source)
  {
    if (source is GH_String ghStr)
      try
      {
        Value = new StreamWrapper(ghStr.Value);
        return true;
      }
      catch
      {
        return false;
      }

    if (source is string str) // Not sure this is needed?
      try
      {
        Value = new StreamWrapper(str);
        return true;
      }
      catch
      {
        return false;
      }

    if (source is StreamWrapper strWrapper)
    {
      Value = strWrapper;
      return true;
    }

    var stream = (source as GH_SpeckleStream)?.Value;
    if (stream == null)
      return false;
    Value = stream;
    return true;
  }

  public override bool CastTo<Q>(ref Q target)
  {
    var type = typeof(Q);

    if (type == typeof(GH_SpeckleStream))
    {
      target = (Q)(object)new GH_SpeckleStream { Value = Value };
      return true;
    }

    if (type == typeof(StreamWrapper))
    {
      target = (Q)(object)Value;
      return true;
    }

    return false;
  }
}
