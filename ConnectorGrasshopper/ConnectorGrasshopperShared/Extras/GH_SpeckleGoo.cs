using System;
using Grasshopper.Kernel.Types;

namespace ConnectorGrashopper.Extras;

public class GH_SpeckleGoo : GH_Goo<object>
{
  public override bool IsValid => true;

  public override string TypeName => Value != null ? Value.GetType().ToString() : "null";

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
