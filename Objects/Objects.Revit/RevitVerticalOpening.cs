using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitVerticalOpening : Opening
  {
    public Element host { get; set; }

    public Dictionary<string, object> parameters { get; set; }
  }
}
