using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Objects.Geometry
{
  public class Circle : Base, ICurve
  {
    public double? radius { get; set; }
    
    public Plane plane { get; set; }
    
    public Interval domain { get; set; }

    public Box boundingBox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double length { get; set; }
    public string linearUnits { get; set; }

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
