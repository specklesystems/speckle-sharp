using Speckle.Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Revit
{
  public class RevitExtrusionRoof : Roof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
