using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Collections;

public class GH_SpeckleCollection : GH_Goo<Collection>
{
  public override IGH_Goo Duplicate() => new GH_SpeckleCollection { m_value = m_value.ShallowCopy() as Collection };

  public override string ToString() => $"Speckle Collection [{m_value?.name ?? "No name"}]";

  public override bool IsValid => m_value != null;
  public override string TypeName => "Speckle Collection";
  public override string TypeDescription => "Represents a collection object from Speckle";

  public override bool CastFrom(object source)
  {
    switch (source)
    {
      case Collection c:
        Value = c;
        return true;
      case GH_SpeckleCollection speckleCollection:
        Value = speckleCollection.Value;
        return true;
      case GH_Goo<Base> goo:
        if (goo.Value is Collection col)
        {
          Value = col;
          return true;
        }

        break;
    }

    return false;
  }

  public override bool CastTo<Q>(ref Q target)
  {
    var success = false;
    var type = typeof(Q);
    if (type == typeof(Collection))
    {
      target = (Q)(object)Value;
      success = true;
    }
    else if (type == typeof(GH_SpeckleCollection))
    {
      target = (Q)(object)new GH_SpeckleCollection { Value = Value };
      success = true;
    }
    else if (type == typeof(GH_SpeckleBase))
    {
      target = (Q)(object)new GH_SpeckleBase { Value = Value };
      success = true;
    }

    return success;
  }
}
