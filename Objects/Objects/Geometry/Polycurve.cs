using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Polycurve : Base, ICurve
  {
    public List<ICurve> segments { get; set; } = new List<ICurve>();
    public Interval domain { get; set; }
    public bool closed { get; set; }
    public Box bbox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double length { get; set; }
    public string units { get; set; }

    public Polycurve()
    {

    }

    public Polycurve(string units = Units.Meters, string applicationId = null)
    {
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
