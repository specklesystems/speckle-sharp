using Newtonsoft.Json;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Point : Base, IGeometry
  {
    public List<double> value { get; set; }

    public Point() { }

    public Point(double x, double y, double z = 0, string applicationId = null)
    {
      this.value = new List<double>() { x, y, z };
      this.applicationId = applicationId;
    }

    [JsonIgnore]
    public double x
    {
      get
      {
        return value[0];
      }
    }
    [JsonIgnore]
    public double y
    {
      get
      {
        return value[1];
      }
    }
    [JsonIgnore]
    public double z
    {
      get
      {
        return value[2];
      }
    }
  }
}
