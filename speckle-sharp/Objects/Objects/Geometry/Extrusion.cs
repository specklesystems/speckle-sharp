using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Extrusion : Base, IHasVolume, IHasArea, IHasBoundingBox
  {
    public bool? capped { get; set; }
    public Base profile { get; set; }
    public Point pathStart { get; set; }
    public Point pathEnd { get; set; }
    public Base pathCurve { get; set; }
    public Base pathTangent { get; set; }
    public List<Base> profiles { get; set; }
    public double? length;

    public Box bbox { get; set; }

    public double area { get; set; }
    public double volume { get; set; }

    public string units { get; set; }

    public Extrusion() { }

    public Extrusion(Base profile, double length, bool capped, string units = Units.Meters, string applicationId = null)
    {
      this.profile = profile;
      this.length = length;
      this.capped = capped;
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
