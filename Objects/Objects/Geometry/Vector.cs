using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Vector : Base, IGeometry
  {
    public List<double> value { get; set; }
    public string units { get; set; }

    public Vector() { }

    public Vector(double x, double y, string units = Units.Meters, string applicationId = null)
    {
      this.value = new List<double>() { x, y, 0 };
      this.applicationId = applicationId;
      this.units = units;
    }

    public Vector(double x, double y, double z, string units = Units.Meters, string applicationId = null)
    {
      this.value = new List<double>() { x, y, z };
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
