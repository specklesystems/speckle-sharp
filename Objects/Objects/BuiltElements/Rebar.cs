using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Objects.BuiltElements
{
  public class Rebar : Base, IDisplayMesh, IHasVolume
  {
    public List<ICurve> curves { get; set; } = new List<ICurve>();

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public string units { get; set; }
    public double volume { get ; set ; }

    public Rebar() { }

    [SchemaInfo("Rebar", "Creates a Speckle rebar", "BIM", "Structure")]
    public Rebar(List<ICurve> curves)
    {
      this.curves = curves;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitRebar : Rebar
  {
    public string family { get; set; }
    public string type { get; set; }
    public string host { get; set; }
    public string barType { get; set; }
    public string barStyle { get; set; }
    public string shape { get; set; } //if this exists, it is shape driven
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitRebar() { }

    [SchemaInfo("RevitRebar", "Creates a Revit rebar from curves.", "Revit", "Structure")]
    public RevitRebar(string family, string type, List<ICurve> curves, string host, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.curves = curves;
      this.parameters = parameters.ToBase();
      this.host = host;
    }
  }
}
