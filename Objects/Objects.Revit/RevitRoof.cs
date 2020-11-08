using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitRoof : Roof, IRevitElement
  {
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }

    public RevitLevel level { get; set; }

  }



  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }
    public new string family { get; set; }
    public string type { get; set; }
    public new Dictionary<string, object> parameters { get; set; }
    public new Dictionary<string, object> typeParameters { get; set; }
    public new string elementId { get; set; }

    public new RevitLevel level { get; set; }

    public new ICurve outline { get; set; }
  }

  public class RevitFootprintRoof : RevitRoof
  {
    public RevitLevel cutOffLevel { get; set; }
    public new string family { get; set; }
    public new string type { get; set; }
    public new Dictionary<string, object> parameters { get; set; }
    public new Dictionary<string, object> typeParameters { get; set; }
    public new string elementId { get; set; }

    public new RevitLevel level { get; set; }

    public new Polycurve outline { get; set; }
  }
}
