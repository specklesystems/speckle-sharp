using Objects.Primitive;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Geometry
{
  public class Circle : Base, ICurve
  {
    public double? radius { get; set; }

    public Plane plane { get; set; }

    public Interval domain { get; set; }

    public Box bbox { get; set; }

    //public Point center { get; set; }

    public double area { get; set; }

    public double length { get; set; }

    public string units { get; set; }

    public Circle()
    {
    }

    public Circle(Plane plane, double radius, string units = Units.Meters, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.applicationId = applicationId;
      this.units = units;
    }

    public List<double> ToList()
    {
      var list = new List<double>();

      list.Add(radius ?? 0);
      list.Add(domain.start ?? 0);
      list.Add(domain.end ?? 1);
      list.AddRange(plane.ToList());

      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.Circle);
      list.Insert(0, list.Count);
      return list;
    }

    public static Circle FromList(List<double> list)
    {
      var circle = new Circle();
      circle.radius = list[2];
      circle.domain = new Interval(list[3], list[4]);
      circle.plane = Plane.FromList(list.GetRange(5, 13));
      circle.units = Units.GetUnitFromEncoding(list[list.Count - 1]);

      return circle;
    }
  }
}
