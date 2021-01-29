using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Geometry
{
  public class Point : Base, IHasBoundingBox
  {
    public List<double> value { get; set; }

    public Box bbox { get; set; }

    public Point() { }

    public Point(double x, double y, double z, string units = Units.Meters, string applicationId = null)
    {
      this.value = new List<double>() { x, y, z };
      this.applicationId = applicationId;
      this.units = units;
    }

    public Point(double x, double y, string units = Units.Meters, string applicationId = null)
    {
      this.value = new List<double>() { x, y, 0 };
      this.applicationId = applicationId;
      this.units = units;
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
