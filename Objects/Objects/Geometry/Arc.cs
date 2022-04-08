using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Arc : Base, IHasBoundingBox, ICurve, IHasArea
  {
    public double? radius { get; set; }
    public double? startAngle { get; set; }

    public double? endAngle { get; set; }

    public double? angleRadians { get; set; }

    /// <summary>
    /// Gets or sets the plane of the <see cref="Arc"/>. The plane origin is the <see cref="Arc"/> center.
    /// </summary>
    public Plane plane { get; set; }

    public Interval domain { get; set; }

    public Point startPoint { get; set; }

    /// <summary>
    /// Gets or sets the point at 0.5 length.
    /// </summary>
    public Point midPoint { get; set; }

    public Point endPoint { get; set; }

    public Box bbox { get; set; }

    public double area { get; set; }

    public double length { get; set; }
    public string units { get; set; }

    public Arc() { }

    public Arc(Plane plane, double radius, double startAngle, double endAngle, double angleRadians, string units = Units.Meters, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.startAngle = startAngle;
      this.endAngle = endAngle;
      this.angleRadians = angleRadians;
      this.applicationId = applicationId;
      this.units = units;
    }

    public List<double> ToList()
    {
      var list = new List<double>();
      list.Add(radius ?? 0);
      list.Add(startAngle ?? 0);
      list.Add(endAngle ?? 0);
      list.Add(angleRadians ?? 0);
      list.Add(domain.start ?? 0);
      list.Add(domain.end ?? 0);

      list.AddRange(plane.ToList());
      list.AddRange(startPoint.ToList());
      list.AddRange(midPoint.ToList());
      list.AddRange(endPoint.ToList());
      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.Arc);
      list.Insert(0, list.Count);
      return list;
    }

    public static Arc FromList(List<double> list)
    {
      var arc = new Arc();

      arc.radius = list[2];
      arc.startAngle = list[3];
      arc.endAngle = list[4];
      arc.angleRadians = list[5];
      arc.domain = new Interval(list[6], list[7]);
      arc.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
      arc.plane = Plane.FromList(list.GetRange(8, 13));
      arc.startPoint = Point.FromList(list.GetRange(21,3), arc.units);
      arc.midPoint = Point.FromList(list.GetRange(24,3), arc.units);
      arc.endPoint = Point.FromList(list.GetRange(27,3), arc.units);
      arc.plane.units = arc.units;

      return arc;
    }
  }
}
