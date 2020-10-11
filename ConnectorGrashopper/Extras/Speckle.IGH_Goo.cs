using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;

namespace ConnectorGrashopper.Extras
{
  public class GH_SpeckleGoo : GH_Goo<object>
  {
    public override bool IsValid => true;

    public override string TypeName => (Value != null ? Value.GetType().ToString() : "null");

    public override string TypeDescription => "A generic goo.";

    public override IGH_Goo Duplicate()
    {
      throw new NotImplementedException();
    }

    public override string ToString()
    {
      return Value?.ToString();
    }
  }

  public class GH_SpeckleBase : GH_Goo<Base>
  {
    public override bool IsValid => true;

    public override string TypeName => "Speckle" + (Value != null && Value.speckle_type == "" ? " Base" : " " + Value?.speckle_type);

    public override string TypeDescription => "A Speckle Object";

    public override object ScriptVariable()
    {
      return Value;
    }

    public override bool CastFrom(object source)
    {
      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      var x = typeof(Q);
      return base.CastTo(ref target);
    }

    public override IGH_Goo Duplicate()
    {
      throw new NotImplementedException();
    }

    public override string ToString()
    {
      return $"{(Value != null && Value.speckle_type == "" ? "Speckle.Base" : Value?.speckle_type)}";
    }
  }

}