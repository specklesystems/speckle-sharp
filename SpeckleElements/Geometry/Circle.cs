using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Circle : Base
  {
    public double? radius { get; set; }
    public Plane plane { get; set; }
    public Point center { get { return plane?.origin; } }
    public Vector normal { get { return plane?.normal; } }
    public Interval domain { get; set; }

    public Circle()
    {
    }

    public Circle(Plane plane, double radius, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.applicationId = applicationId;
    }
  }
}
