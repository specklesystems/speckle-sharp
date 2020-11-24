using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Revit
{
  [SchemaIgnore]
  public class RevitRoom : Element, IRoom, IRevit
  {
    public string name { get; set; }
    public string number { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public List<ICurve> voids { get; set; }
    public ICurve outline { get; set; }
    public Point basePoint { get; set; }
    public RevitLevel level { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public string elementId { get; set; }
  }
}
