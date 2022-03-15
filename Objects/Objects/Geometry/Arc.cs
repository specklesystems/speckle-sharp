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

    public Arc()
    {
    }

    public Arc(Plane plane, double radius, double startAngle, double endAngle, double angleRadians,
      string units = Units.Meters, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.startAngle = startAngle;
      this.endAngle = endAngle;
      this.angleRadians = angleRadians;
      this.applicationId = applicationId;
      this.units = units;
    }

    /// <summary>
    /// Initialise an `Arc` using the arc angle and the start and end points.
    /// The radius, midpoint, start angle, and end angle will be calculated.
    /// </summary>
    /// <param name="startPoint">The start point of the arc</param>
    /// <param name="endPoint">The end point of the arc</param>
    /// <param name="angleRadians">The arc angle</param>
    /// <param name="units">Units (defaults to "m")</param>
    /// <param name="applicationId">ID given to the arc in the authoring programme (defaults to null)</param>
    public Arc(Point startPoint, Point endPoint, double angleRadians, string units = Units.Meters,
      string applicationId = null)
    {
      this.units = units;
      this.startPoint = startPoint;
      this.endPoint = endPoint;
      this.angleRadians = angleRadians;
      this.applicationId = applicationId;

      var chordMidpoint = Point.Midpoint(startPoint, endPoint);
      var chordLength = Point.Distance(startPoint, endPoint);
      var chordAngle = angleRadians;
      if ( chordAngle > Math.PI )
        chordAngle -= Math.PI * 2;
      else if ( chordAngle < -Math.PI )
        chordAngle += Math.PI * 2;
      radius = chordLength / Math.Sqrt(2 - 2 * Math.Cos(chordAngle));
      var radSqr = Math.Pow(( double )radius, 2);
      var dir = chordAngle < 0 ? -1 : 1;
      var circleCentre = new Point
      {
        x = chordMidpoint.x + dir * Math.Sqrt(radSqr - Math.Pow(chordLength * 0.5, 2)) *
          ( startPoint.y - endPoint.y ) / chordLength,
        y = chordMidpoint.y + dir * Math.Sqrt(radSqr - Math.Pow(chordLength * 0.5, 2)) *
          ( startPoint.x - endPoint.x ) / chordLength,
        z = startPoint.z, units = units
      };
      var unitR = chordAngle == angleRadians ?  chordMidpoint - circleCentre : circleCentre - chordMidpoint;
      unitR /= Point.Distance(circleCentre, chordMidpoint);
      midPoint = circleCentre + unitR * ( double )radius;
      startAngle = Math.Tan(( startPoint.y - circleCentre.y ) / ( startPoint.x - circleCentre.x )) * 180 / Math.PI %
                   360;
      if ( startPoint.x > circleCentre.x && startPoint.y < circleCentre.y )       // Q4
        startAngle *= -1;
      else if ( startPoint.x < circleCentre.x && startPoint.y < circleCentre.y )  // Q3
        startAngle += 180;
      else if ( startPoint.x < circleCentre.x && startPoint.y > circleCentre.y )  // Q2
        startAngle = 180 - startAngle;
      endAngle = startAngle + angleRadians * 180 / Math.PI;
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

      arc.radius = list[ 2 ];
      arc.startAngle = list[ 3 ];
      arc.endAngle = list[ 4 ];
      arc.angleRadians = list[ 5 ];
      arc.domain = new Interval(list[ 6 ], list[ 7 ]);
      arc.units = Units.GetUnitFromEncoding(list[ list.Count - 1 ]);
      arc.plane = Plane.FromList(list.GetRange(8, 13));
      arc.startPoint = Point.FromList(list.GetRange(21, 3), arc.units);
      arc.midPoint = Point.FromList(list.GetRange(24, 3), arc.units);
      arc.endPoint = Point.FromList(list.GetRange(27, 3), arc.units);
      arc.plane.units = arc.units;

      return arc;
    }
  }
}