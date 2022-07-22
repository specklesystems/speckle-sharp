using System;
using System.Collections.Generic;
using System.Text;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry
{
  public class Vector : Base, IHasBoundingBox, ITransformable<Vector>
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
        x = value[ 0 ];
        y = value[ 1 ];
        z = value.Count > 2 ? value[ 2 ] : 0;
      }
    }

    public Box bbox { get; set; }

    public string units { get; set; }

    public Vector()
    {
    }

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

    public Vector(Point point, string applicationId = null) : this(point.x, point.y, point.z, point.units,
      applicationId)
    {
    }

    public List<double> ToList()
    {
      return new List<double>() { x, y, z };
    }

    public static Vector FromList(List<double> list, string units) => new Vector(list[ 0 ], list[ 1 ], list[ 2 ], units);

    public double x { get; set; }

    public double y { get; set; }

    public double z { get; set; }

    public static Vector operator /(Vector vector, double val) => new Vector(
      vector.x / val,
      vector.y / val,
      vector.z / val, vector.units);

    public static Vector operator *(Vector vector, double val) => new Vector(
      vector.x * val,
      vector.y * val,
      vector.z * val, vector.units
    );

    public static Vector operator +(Vector vector1, Vector vector2) => new Vector(
      vector1.x + vector2.x,
      vector1.y + vector2.y,
      vector1.z + vector2.z, vector1.units);

    public static Vector operator -(Vector vector1, Vector vector2) => new Vector(
      vector1.x - vector2.x,
      vector1.y - vector2.y,
      vector1.z - vector2.z, vector1.units);

    /// <summary>
    /// Gets the Euclidean length of this vector.
    /// </summary>
    /// <returns>Length of the vector.</returns>
    public double Length => Math.Sqrt(DotProduct(this, this));

    /// <summary>
    /// Gets the scalar product (dot product) of two given vectors
    /// Dot product = u1*v1 + u2*v2 + u3*v3.
    /// </summary>
    /// <param name="u">First vector.</param>
    /// <param name="v">Second vector.</param>
    /// <returns>Numerical value of the dot product.</returns>
    public static double DotProduct(Vector u, Vector v) =>
      u.x * v.x + u.y * v.y + u.z * v.z;

    /// <summary>
    /// Computes the vector product (cross product) of two given vectors
    /// Cross product = { u2 * v3 - u3 * v2; u3 * v1 - u1 * v3; u1 * v2 - u2 * v1 }.
    /// </summary>
    /// <param name="u">First vector.</param>
    /// <param name="v">Second vector.</param>
    /// <returns>Vector result of the cross product.</returns>
    public static Vector CrossProduct(Vector u, Vector v)
    {
      var x = u.y * v.z - u.z * v.y;
      var y = u.z * v.x - u.x * v.z;
      var z = u.x * v.y - u.y * v.x;

      return new Vector(x, y, z);
    }

    /// <summary>
    /// Divides this vector by it's euclidean length.
    /// </summary>
    public void Unitize()
    {
      var length = this.Length;
      this.x /= length;
      this.y /= length;
      this.z /= length;
    }

    /// <summary>
    /// Returns a normalized copy of this vector.
    /// </summary>
    /// <returns>A copy of this vector unitized.</returns>
    public Vector Unit()
    {
      var length = this.Length;
      var x = this.x / length;
      var y = this.y / length;
      var z = this.z / length;
      return new Vector(x, y, z);
    }

    public bool TransformTo(Transform transform, out Vector vector)
    {
      vector = transform.ApplyToVector(this);
      return true;
    }

    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      transformed = transform.ApplyToVector(this);
      return true;
    }
  }
}