using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Geometry
{
  public class Extrusion : Base, IGeometry
  {
    public bool? capped { get; set; }
    public Base profile { get; set; }
    public Point pathStart { get; set; }
    public Point pathEnd { get; set; }
    public Base pathCurve { get; set; }
    public Base pathTangent { get; set; }
    public List<Base> profiles { get; set; }
    public double? length;
    public Extrusion()
    {

    }

    public Extrusion(Base profile, double length, bool capped, string applicationId = null)
    {
      this.profile = profile;
      this.length = length;
      this.capped = capped;
      this.applicationId = applicationId;
    }
  }
}
