using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class CableTray : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve baseCurve { get; set; }
    public double width { get; set; }
    public double height { get; set; }
    public double length { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public CableTray() { }

  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitCableTray : CableTray
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitCableTray() { }

  }
}
