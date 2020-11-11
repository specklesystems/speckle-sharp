using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Box : Base, I3DGeometry
  {
    public Plane basePlane { get; set; }
    public Interval xSize { get; set; }
    public Interval ySize { get; set; }
    public Interval zSize { get; set; }
    public string linearUnits { get; set; }
    public Box boundingBox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double volume { get; set; }

    public Box()
    {

    }

    public Box(Plane basePlane, Interval xSize, Interval ySize, Interval zSize, string applicationId = null)
    {
      this.basePlane = basePlane;
      this.xSize = xSize;
      this.ySize = ySize;
      this.zSize = zSize;
      this.applicationId = applicationId;
    }
  }
}
