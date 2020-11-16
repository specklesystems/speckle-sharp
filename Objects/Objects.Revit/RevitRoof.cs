using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{

  public class RevitExtrusionRoof : RevitElement, IRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
  }

  public class RevitFootprintRoof : RevitElement, IRoof
  {
    public RevitLevel cutOffLevel { get; set; }
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
  }
}
