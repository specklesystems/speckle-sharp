using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitFootprintRoof : Roof
  {
    public Level cutOffLevel { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
