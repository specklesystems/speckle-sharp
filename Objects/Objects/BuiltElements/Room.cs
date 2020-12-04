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
    public string number { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public Level level { get; set; }
    public Point center { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    public ICurve outline { get; set; }
    public Room() { }

  }
}
