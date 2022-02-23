using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Objects.Geometry
{
  public class Box : Base, IHasVolume, IHasArea, IHasBoundingBox
  {
    public Plane basePlane { get; set; }

    public Interval xSize { get; set; }

    public Interval ySize { get; set; }

    public Interval zSize { get; set; }

    public Box bbox { get; }
    
    public double area { get; set; }

    public double volume { get; set; }
    public string units { get; set; }

    public Box() { }

    public Box(Plane basePlane, Interval xSize, Interval ySize, Interval zSize, string units = Units.Meters, string applicationId = null)
    {
      this.basePlane = basePlane;
      this.xSize = xSize;
      this.ySize = ySize;
      this.zSize = zSize;
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
