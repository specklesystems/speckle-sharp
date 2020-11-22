using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Plane : Base, I2DGeometry
  {
    public Point origin { get; set; }
    public Vector normal { get; set; }
    public Vector xdir { get; set; }
    public Vector ydir { get; set; }
    public Box boundingBox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double length { get; set; }
    public string units { get; set; }

    public Plane()
    {

    }
    public Plane(Point origin, Vector normal,
      Vector xDir, Vector yDir, string units = Units.Meters, string applicationId = null)
    {
      this.origin = origin;
      this.normal = normal;
      this.xdir = xDir;
      this.ydir = yDir;
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
