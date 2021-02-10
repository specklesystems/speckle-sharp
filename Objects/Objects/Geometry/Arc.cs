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
  }
}
