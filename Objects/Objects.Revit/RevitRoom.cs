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
  public class RevitRoom : Base, IBaseRevitElement, IRoom, IHasArea, IHasVolume
  {
    public string name { get; set; }
    
    public string number { get; set; }

    [SchemaIgnore]
    public double area { get; set; }
    
    [SchemaIgnore]
    public double volume { get; set; }

    [SchemaOptional]
    public List<ICurve> voids { get; set; }
    
    public ICurve outline { get; set; }

    [SchemaOptional]
    public Point basePoint { get; set; }

    [SchemaOptional]
    public RevitLevel level { get; set; }
    
    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }
    
    [SchemaIgnore]
    public string elementId { get; set; }
  }
}
