using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Objects.BuiltElements
{
  public class Room : Base, IHasArea, IHasVolume
  {
    public string name { get; set; }

    [SchemaOptional]
    public string number { get; set; }
    
    [SchemaOptional]
    public double area { get; set; }

    [SchemaOptional]
    public double volume { get; set; }

    [SchemaOptional]
    public Level level { get; set; }

    [SchemaOptional]
    public Point center { get; set; }
    
    [SchemaOptional]
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    
    public ICurve outline { get; set; }

    public Room() { }

  }
}
