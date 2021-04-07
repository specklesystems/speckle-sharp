using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Plane : Base
  {
    public Point origin { get; set; }

    public Vector normal { get; set; }

    public Vector xdir { get; set; }

    public Vector ydir { get; set; }

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

    public List<double> ToList()
    {
      var list = new List<double>();

      list.AddRange(origin.ToList());
      list.AddRange(normal.ToList());
      list.AddRange(xdir.ToList());
      list.AddRange(ydir.ToList());

      return list;
    }

    public static Plane FromList(List<double> list)
    {
      var plane = new Plane();

      plane.origin = Point.FromList(list.GetRange(0, 3));
      plane.normal = Vector.FromList(list.GetRange(3, 3));
      plane.xdir = Vector.FromList(list.GetRange(6, 3));
      plane.ydir = Vector.FromList(list.GetRange(9, 3));

      return plane;
    }
  }
}
