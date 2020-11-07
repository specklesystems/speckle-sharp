using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitFloor : Floor
  {
    public Dictionary<string, object> parameters { get; set; }
  }
}
