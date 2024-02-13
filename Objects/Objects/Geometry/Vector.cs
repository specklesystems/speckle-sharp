using System;
using System.Collections.Generic;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry;

/// <summary>
/// A 3-dimensional vector
/// </summary>
public class Vector : Base, IHasBoundingBox, ITransformable<Vector>
{
  /// <inheritdoc/>
  public Vector() { }

  /// <summary>
  /// Constructs a new 2D <see cref="Vector"/> from it's X and Y coordinates.
  /// </summary>
  /// <param name="x">The x coordinate of the vector</param>
  /// <param name="y">The y coordinate of the vector</param>
  /// <param name="units">The units the coordinates are in.</param>
  /// <param name="applicationId">The unique application ID of the object.</param>
  public Vector(double x, double y, string units = Units.Meters, string? applicationId = null)
  {
    this.x = x;
    this.y = y;
    z = 0;
    this.applicationId = applicationId;
    this.units = units;
  }

  /// <summary>
  /// Constructs a new 2D <see cref="Vector"/> from it's X and Y coordinates.
  /// </summary>
  /// <param name="x">The x coordinate of the vector</param>
  /// <param name="y">The y coordinate of the vector</param>
  /// <param name="z">The y coordinate of the vector</param>
  /// <param name="units">The units the coordinates are in.</param>
  /// <param name="applicationId">The unique application ID of the object.</param>
  public Vector(double x, double y, double z, string units = Units.Meters, string? applicationId = null)
  {
    this.x = x;
    this.y = y;
    this.z = z;
    this.applicationId = applicationId;
    this.units = units;
  }

  /// <summary>
  /// Constructs a new <see cref="Vector"/> from a <see cref="Point"/>
  /// </summary>
  /// <param name="point">The point whose coordinates will be used</param>
  /// <param name="applicationId">The unique application ID of the object.</param>
  public Vector(Point point, string? applicationId = null)
    : this(point.x, point.y, point.z, point.units ?? Units.None, applicationId) { }

  /// <summary>
  /// Gets or sets the coordinates of the vector
  /// </summary>
  [
    JsonProperty(NullValueHandling = NullValueHandling.Ignore),
    Obsolete("Use X,Y,Z fields to access coordinates instead", true)
  ]
  public List<double> value
  {
#pragma warning disable CS8603 // Possible null reference return.
    get => null;
#pragma warning restore CS8603 // Possible null reference return.
    set
    {
      x = value[0];
      y = value[1];
      z = value.Count > 2 ? value[2] : 0;
    }
  }

  /// <summary>
  /// The unit's this <see cref="Vector"/> is in.
  /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
  /// </summary>
  public string units { get; set; } = Units.None;

  /// <summary>
  /// The x coordinate of the vector.
  /// </summary>
  public double x { get; set; }

  /// <summary>
  /// The y coordinate of the vector.
  /// </summary>
  public double y { get; set; }

  /// <summary>
  /// The z coordinate of the vector.
  /// </summary>
  public double z { get; set; }

  /// <summary>
  /// Gets the Euclidean length of this vector.
  /// </summary>
  /// <returns>Length of the vector.</returns>
  [JsonIgnore]
  public double Length => Math.Sqrt(DotProduct(this, this));

  /// <inheritdoc/>
  public Box bbox { get; set; }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out Vector transformed)
  {
    var m = transform.matrix;
    var tX = x * m.M11 + y * m.M12 + z * m.M13;
    var tY = x * m.M21 + y * m.M22 + z * m.M23;
    var tZ = x * m.M31 + y * m.M32 + z * m.M33;
    transformed = new Vector(tX, tY, tZ, units, applicationId);
    return true;
  }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    _ = TransformTo(transform, out Vector vec);
    transformed = vec;
    return true;
  }

  /// <summary>
  /// Returns the coordinates of this <see cref="Vector"/> as a list of numbers
  /// </summary>
  /// <returns>A list of coordinates {x, y, z} </returns>
  public List<double> ToList()
  {
    return new List<double> { x, y, z };
  }

  /// <summary>
  /// Creates a new vector based on a list of coordinates and the unit they're drawn in.
  /// </summary>
  /// <param name="list">The list of coordinates {x, y, z}</param>
  /// <param name="units">The units the coordinates are in</param>
  /// <returns>A new <see cref="Vector"/> with the provided coordinates.</returns>
  public static Vector FromList(List<double> list, string units)
  {
    return new Vector(list[0], list[1], list[2], units);
  }

  /// <summary>
  /// Divides a vector by a numerical value. This will divide each coordinate by the provided value.
  /// </summary>
  /// <param name="vector">The vector to divide</param>
  /// <param name="val">The value to divide by</param>
  /// <returns>The resulting <see cref="Vector"/></returns>
  public static Vector operator /(Vector vector, double val) =>
    new(vector.x / val, vector.y / val, vector.z / val, vector.units);

  /// <summary>
  /// Multiplies a vector by a numerical value. This will multiply each coordinate by the provided value.
  /// </summary>
  /// <param name="vector">The vector to multiply</param>
  /// <param name="val">The value to multiply by</param>
  /// <returns>The resulting <see cref="Vector"/></returns>
  public static Vector operator *(Vector vector, double val) =>
    new(vector.x * val, vector.y * val, vector.z * val, vector.units);

  /// <summary>
  /// Adds two vectors by adding each of their coordinates.
  /// </summary>
  /// <param name="vector1">The first vector</param>
  /// <param name="vector2">The second vector</param>
  /// <returns>The resulting <see cref="Vector"/></returns>
  public static Vector operator +(Vector vector1, Vector vector2) =>
    new(vector1.x + vector2.x, vector1.y + vector2.y, vector1.z + vector2.z, vector1.units);

  /// <summary>
  /// Subtracts two vectors by subtracting each of their coordinates.
  /// </summary>
  /// <param name="vector1">The first vector</param>
  /// <param name="vector2">The second vector</param>
  /// <returns>The resulting <see cref="Vector"/></returns>
  public static Vector operator -(Vector vector1, Vector vector2) =>
    new(vector1.x - vector2.x, vector1.y - vector2.y, vector1.z - vector2.z, vector1.units);

  /// <summary>
  /// Gets the scalar product (dot product) of two given vectors
  /// Dot product = u1*v1 + u2*v2 + u3*v3.
  /// </summary>
  /// <param name="u">First vector.</param>
  /// <param name="v">Second vector.</param>
  /// <returns>Numerical value of the dot product.</returns>
  public static double DotProduct(Vector u, Vector v)
  {
    return u.x * v.x + u.y * v.y + u.z * v.z;
  }

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

  public static double Angle(Vector u, Vector v)
  {
    return Math.Acos(DotProduct(u, v) / (u.Length * v.Length));
  }

  [Obsolete("Renamed to " + nameof(Normalize), true)]
  public void Unitize()
  {
    Normalize();
  }

  /// <summary>
  /// Compute and return a unit vector from this vector
  /// </summary>
  /// <returns>a normalized unit vector</returns>
  public void Normalize()
  {
    var length = Length;
    x /= length;
    y /= length;
    z /= length;
  }

  /// <summary>
  /// Inverses the direction of the vector, equivalent to multiplying by -1
  /// </summary>
  /// <returns>A pointing in the opposite direction</returns>
  public Vector Negate()
  {
    x *= -1;
    y *= -1;
    z *= -1;
    return this;
  }

  /// <summary>
  /// Returns a normalized copy of this vector.
  /// </summary>
  /// <returns>A copy of this vector unitized.</returns>
  public Vector Unit()
  {
    return this / Length;
  }

  public static Vector Divide(Vector left, Vector right)
  {
    throw new NotImplementedException();
  }
}
