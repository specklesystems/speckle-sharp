using System;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Other;

namespace Objects.Geometry
{
  public class Point : Base, IHasBoundingBox, ITransformable<Point>
  {
    /// <summary>
    /// OBSOLETE - This is just here for backwards compatibility.
    /// You should not use this for anything. Access coordinates using x, y, z properties.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore), Obsolete("Use x,y,z properties")]
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

    public Point(double x, double y, double z = 0d, string units = Units.Meters, string applicationId = null)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.applicationId = applicationId;
      this.units = units;
    }
    

    public double x { get; set; }

    public double y { get; set; }

    public double z { get; set; }

    public string units { get; set; }

    public List<double> ToList() => new List<double>{x, y, z};

    public static Point FromList(IList<double> list, string units) => new Point(list[0], list[1], list[2], units);

    public void Deconstruct(out double x, out double y, out double z, out string units)
    {
      Deconstruct(out x, out y, out z);
      units = this.units;
    }

    public void Deconstruct(out double x, out double y, out double z)
    {
      x = this.x;
      y = this.y;
      z = this.z;
    }

    public bool TransformTo(Transform transform, out Point point)
    {
      point = transform.ApplyToPoint(this);
      return true;
    }
  }
}