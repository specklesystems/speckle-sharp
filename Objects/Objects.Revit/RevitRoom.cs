using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitRoom : Room, IRevit
  {
    public Point basePoint { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public string elementId { get; set; }
    public string type { get; set; }
    public RevitLevel level { get; set; }

    public new ICurve outline { get; set; }
  }
}
