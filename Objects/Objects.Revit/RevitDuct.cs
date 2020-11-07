using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitDuct : Duct
  {
    public string family { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
