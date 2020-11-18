using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Revit
{
  public class RevitRoom : Element, IRoom, IRevit
  {
    public string name { get; set; }
    public string number { get; set; }
    [SchemaIgnore]
    public double area { get; set; }

    [SchemaIgnore]
    public double volume { get; set; }
    public List<ICurve> voids { get; set; }
    public ICurve outline { get; set; }
    public Point basePoint { get; set; }
    public string level { get; set; }
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }
}
