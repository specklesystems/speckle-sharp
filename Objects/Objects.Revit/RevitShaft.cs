using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitShaft : Opening
  {
    public Level topLevel { get; set; }

    public Dictionary<string, object> parameters { get; set; }
  }
}
