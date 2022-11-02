using System;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Conduit : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve baseCurve { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Conduit() { }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitConduit : Conduit
  {
    public string family { get; set; }

    public string type { get; set; }

    public Level level { get; set; }

    public Base parameters { get; set; }

    public string elementId { get; set; }

    public RevitConduit() { }
  }
}
