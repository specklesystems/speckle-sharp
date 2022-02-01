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
    public List<string> shapes { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitRebar() { }

  }
}
