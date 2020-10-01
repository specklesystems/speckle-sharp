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

    [JsonIgnore]
    public Point center { get { return plane?.origin; } }

    [JsonIgnore]
    public Vector normal { get { return plane?.normal; } }

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
