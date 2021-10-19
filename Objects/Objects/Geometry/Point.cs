using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Geometry
{
  public class Point : Base, IHasBoundingBox
  {
    /// <summary>
    /// OBSOLETE - This is just here for backwards compatibility.
    /// You should not use this for anything. Access coordinates using X,Y,Z fields.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<double> value
    {
      get { return null; }
      set
      {
        x = value[0];
        y = value[1];
        z = value.Count > 2 ? value[2] : 0;
      }
    }

    public Box bbox { get; set; }

    public Point()
    {
    }

    public Point(double x, double y, double z, string units = Units.Meters, string applicationId = null)
    {
      this.value = new List<double>() {x, y, z};
      this.applicationId = applicationId;
      this.units = units;
    }

    public Point(double x, double y, string units = Units.Meters, string applicationId = null)
    {
      this.value = new List<double>() {x, y, 0};
      this.applicationId = applicationId;
      this.units = units;
    }

    public double x { get; set; }

    public double y { get; set; }

    public double z { get; set; }

    public string units { get; set; }

    public List<double> ToList() => new List<double>() { x, y, z };

    public static Point FromList(List<double> list, string units) => new Point(list[0], list[1], list[2]);
  }
}