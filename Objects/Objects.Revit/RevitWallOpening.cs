using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitWallOpening : Opening
  {
    public RevitWall host { get; set; }

    public Dictionary<string, object> parameters { get; set; }
  }
}
