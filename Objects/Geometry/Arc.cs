using Speckle.Objects.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Geometry
{
  public class Arc : Base, ICurve
  {
    public double? radius { get; set; }

    public double? startAngle { get; set; }

    public double? endAngle { get; set; }

    public double? angleRadians { get; set; }

    public Plane plane { get; set; }

    public Interval domain { get; set; }

    public Point startPoint { get; set; }

    public Point midPoint { get; set; }

    public Point endPoint { get; set; }
    public Arc()
    {

    }
    public Arc(Plane plane, double radius, double startAngle, double endAngle, double angleRadians, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.startAngle = startAngle;
      this.endAngle = endAngle;
      this.angleRadians = angleRadians;
      this.applicationId = applicationId;
    }
  }
}
