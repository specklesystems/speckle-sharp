using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Vector : Base, IGeometry
  {
    public List<double> value { get; set; }

    public Vector() { }

    public Vector(double x, double y, double z = 0, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.value = new List<double>() { x, y, z };
      this.applicationId = applicationId;
    }
  }
}
