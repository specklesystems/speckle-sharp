using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.Other;

namespace Objects.Geometry
{
  public class Plane : Base, ITransformable<Plane>
  {
    public Point origin { get; set; }

    public Vector normal { get; set; }

    public Vector xdir { get; set; }

    public Vector ydir { get; set; }

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

    public List<double> ToList()
    {
      var list = new List<double>();

      list.AddRange(origin.ToList());
      list.AddRange(normal.ToList());
      list.AddRange(xdir.ToList());
      list.AddRange(ydir.ToList());
      list.Add(Units.GetEncodingFromUnit(units));

      return list;
    }

    public static Plane FromList(List<double> list)
    {
      var plane = new Plane();

      var units = Units.GetUnitFromEncoding(list[list.Count - 1]);
      plane.origin = new Point(list[0], list[1], list[2], units);
      plane.normal = new Vector(list[3], list[4], list[5], units);
      plane.xdir = new Vector(list[6], list[7], list[8], units);
      plane.ydir = new Vector(list[9], list[10], list[11], units);

      return plane;
    }

    public bool TransformTo(Transform transform, out Plane plane)
    {
      plane = new Plane
      {
        origin = transform.ApplyToPoint(origin),
        normal = transform.ApplyToVector(normal),
        xdir = transform.ApplyToVector(xdir),
        ydir = transform.ApplyToVector(ydir),
        applicationId = applicationId,
        units = units
      };

      return true;
    }

    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      var res = TransformTo(transform, out Plane plane);
      transformed = plane;
      return res;
    }
  }
}
