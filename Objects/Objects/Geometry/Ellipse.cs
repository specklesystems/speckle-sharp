using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Ellipse : Base, ICurve
  {
    public double? firstRadius { get; set; }
    public double? secondRadius { get; set; }
    public Plane plane { get; set; }
    public Interval domain { get; set; }
    public Interval trimDomain { get; set; }

    public Box boundingBox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double length { get; set; }
    public string linearUnits { get; set; }

    public Ellipse()
    {
    }

    public Ellipse(Plane plane, double radius1, double radius2, string applicationId = null)
      : this(plane, radius1, radius2, new Interval(0, 2 * Math.PI), null)
    {
    }

    public Ellipse(Plane plane, double radius1, double radius2, Interval domain, Interval trimDomain,
      string applicationId = null)
    {
      this.plane = plane;
      this.firstRadius = radius1;
      this.secondRadius = radius2;
      this.domain = domain;
      this.trimDomain = trimDomain;
      this.applicationId = applicationId;
    }
  }
}