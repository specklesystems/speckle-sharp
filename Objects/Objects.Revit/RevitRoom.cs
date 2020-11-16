using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  public class RevitRoom : IRoom, IRevit
  {
    public string name { get; set; }
    public string number { get; set; }
    [SchemaBuilderIgnore]
    public double area { get; set; }

    [SchemaBuilderIgnore]
    public double volume { get; set; }
    public List<ICurve> voids { get; set; }
    public ICurve outline { get; set; }
    public Point basePoint { get; set; }
    public RevitLevel level { get; set; }
    public Dictionary<string, object> parameters { get; set; }

    [SchemaBuilderIgnore]
    public string elementId { get; set; }
  }
}
