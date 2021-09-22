using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry
{
  public class Vector : Base, IHasBoundingBox
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

    public string units { get; set; }

    public Vector() { }

    public Vector(double x, double y, string units = Units.Meters, string applicationId = null)
    {
      this.x = x;
      this.y = y;
      this.z = 0;
      this.applicationId = applicationId;
      this.units = units;
    }

    public Vector(double x, double y, double z, string units = Units.Meters, string applicationId = null)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.applicationId = applicationId;
      this.units = units;
    }

    public List<double> ToList()
    {
      return new List<double>() { x, y, z };
    }

    public static Vector FromList(List<double> list, string units) => new Vector(list[0], list[1], list[2]);

    public double x
    {
      get;
      set;
    }
    
    public double y
    {
      get;
      set;
    }
    
    public double z
    {
      get;
      set;
    }

    //Overloading operators
    public static Vector operator +(Vector a) => a;
    public static Vector operator -(Vector a) => new Vector(-a.x, -a.y, -a.z, a.units);
    public static Vector operator +(Vector a, Vector b)
    {
      if (a.units == b.units)
      {
        return new Vector(a.x + b.x, a.y + b.y, a.z + b.z, a.units);
      }
      else
      {
        return null;
      }
    }
    public static Vector operator -(Vector a, Vector b)
    {
      if (a.units == b.units)
      {
        return new Vector(a.x - b.x, a.y - b.y, a.z - b.z, a.units);
      }
      else
      {
        return null;
      }
    }
    public static Vector operator *(Vector a, Vector b)
    {
      if (a.units == b.units)
      {
        return new Vector(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x, a.units);
      }
      else
      {
        return null;
      }
    }
    public static Vector operator *(double s, Vector a) => new Vector(s * a.x, s * a.y, s * a.z, a.units);
    public static Vector operator *(Vector a, double s) => s * a;
  }
}
