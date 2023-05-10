using System.Collections;
using ConnectorGrasshopper.Collections;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Extras;

public class GH_SpeckleBase : GH_Goo<Base>
{
  public GH_SpeckleBase()
  {
    m_value = null;
  }

  public GH_SpeckleBase(Base internal_data)
    : base(internal_data)
  {
    m_value = internal_data;
  }

  public GH_SpeckleBase(GH_Goo<Base> other)
    : base(other)
  {
    m_value = other.Value;
  }

  public override bool IsValid => Value != null;

  public override string TypeName => ToString();

  public override string TypeDescription => "A Speckle Object";

  public override object ScriptVariable()
  {
    return Value;
  }

  public override bool CastFrom(object source)
  {
    Base @base;
    switch (source)
    {
      case Base _base:
        @base = _base;
        break;
      case GH_SpeckleBase speckleBase:
        @base = speckleBase.Value;
        break;
      case GH_SpeckleCollection speckleCollection:
        @base = speckleCollection.Value;
        break;
      case GH_Goo<Base> goo:
        @base = goo.Value;
        break;
      case IDictionary dict:
        @base = new Base();
        foreach (DictionaryEntry kvp in dict)
        {
          if (kvp.Key is not string s)
            return false;
          @base[s] = kvp.Value;
        }
        break;
      default:
        return false;
    }

    Value = @base;
    return true;
  }

  public override bool CastTo<Q>(ref Q target)
  {
    var type = typeof(Q);
    var success = false;
    if (type == typeof(GH_SpeckleBase))
    {
      target = (Q)(object)new GH_SpeckleBase { Value = Value };
      success = true;
    }
    else if (type == typeof(GH_SpeckleCollection) && Value is Collection collection)
    {
      target = (Q)(object)new GH_SpeckleCollection { Value = collection };
      success = true;
    }
    return success;
  }

  public override IGH_Goo Duplicate()
  {
    return new GH_SpeckleBase { Value = Value.ShallowCopy() };
  }

  public override string ToString()
  {
    if (Value == null)
      return "";
    var name = Value["Name"] ?? Value["name"];

    if (Value.GetType().IsSubclassOf(typeof(Base)))
    {
      var baseString = $"Speckle {Value.GetType().Name}";
      if (name != null)
        baseString += $" [{name}]";
      return baseString;
    }
    return "Speckle Object";

    //return $"{(Value != null && Value.speckle_type == "" ? "Speckle.Base" : Value?.speckle_type)}";
  }
}
