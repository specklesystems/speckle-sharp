using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Ellipse : Geometry
  {
    public double? firstRadius { get; set; }
    public double? secondRadius { get; set; }
    public Plane plane { get; set; }
    public Interval domain { get; set; }

    public Ellipse()
    {

    }

    public Ellipse(Plane plane, double radius1, double radius2, string applicationId = null)
    {
      this.plane = plane;
      this.firstRadius = radius1;
      this.secondRadius = radius2;
      this.applicationId = applicationId;
    }
  }
}
