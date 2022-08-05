using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Extras
{
  public class GH_SpeckleBase : GH_Goo<Base>
  {
    public GH_SpeckleBase()
    {
      Value = null;
    }

    public GH_SpeckleBase(Base internal_data) : base(internal_data)
    {
      Value = internal_data;
    }

    public GH_SpeckleBase(GH_Goo<Base> other) : base(other)
    {
      Value = other.Value;
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
      Base @base = null;
      var type = source.GetType();

      if (source == null)
        return false;

      if (source is Base _base)
      {
        @base = _base;
      }
      else if (source is GH_SpeckleBase speckleBase)
      {
        @base = speckleBase.Value;
      }
      else if (source is GH_Goo<Base> goo)
      {
        @base = goo.Value;
      } 
      else if(typeof(IDictionary).IsAssignableFrom(source.GetType()))
      {
        var dict = source as IDictionary;
        @base = new Base();
        foreach(DictionaryEntry kvp in dict)
        {
          if (!(kvp.Key is string s)) return false;
          @base[(string)kvp.Key] = kvp.Value;
        }
      } 
      
      Value = @base;
      return true;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (!(target is GH_SpeckleBase))
        return false;

      target = (Q) (object) new GH_SpeckleBase {Value = Value};
      return true;
    }

    public override IGH_Goo Duplicate()
    {
      return new GH_SpeckleBase {Value = Value.ShallowCopy()};
    }

    public override string ToString()
    {
      if (Value == null) return "";
      if (Value.GetType().IsSubclassOf(typeof(Base)))
        return $"Speckle {Value.GetType().Name}";
      return "Speckle Object";
      
      //return $"{(Value != null && Value.speckle_type == "" ? "Speckle.Base" : Value?.speckle_type)}";
    }
  }

}
