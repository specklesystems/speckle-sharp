using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaIgnore]

  public class RevitRoof : RevitElement, IRoof
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

  }
  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }
  }

  public class RevitFootprintRoof : RevitRoof
  {
    public string cutOffLevel { get; set; }
  }
}
