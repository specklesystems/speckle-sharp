using System;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Other;

namespace Objects.Geometry
{
  /// <summary>
  /// A 3-dimensional point
  /// </summary>
  /// <remarks>
  /// TODO: The Point class does not override the Equality operator, which means that there may be cases where `Equals` is used instead of `==`, as the comparison will be done by reference, not value.
  /// </remarks>
  public class Point : Base, IHasBoundingBox, ITransformable<Point>
  {
    /// <summary>
    /// Gets or sets the coordinates of the <see cref="Point"/>
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [Obsolete("Use x,y,z properties instead", true)]
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

    /// <inheritdoc/>
    public Box bbox { get; set; }
    
    /// <inheritdoc/>
    public Point()
    {
    }
    
    /// <summary>
    /// Constructs a new <see cref="Point"/> from a set of coordinates and it's units.
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    /// <param name="z">The z coordinate</param>
    /// <param name="units">The units the point's coordinates are in.</param>
    /// <param name="applicationId">The object's unique application ID</param>
    public Point(double x, double y, double z = 0d, string units = Units.Meters, string applicationId = null)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.applicationId = applicationId;
      this.units = units;
    }
    
    /// <summary>
    /// Constructs a new <see cref="Point"/> from a <see cref="Vector"/>
    /// </summary>
    /// <param name="vector">The Vector whose coordinates will be used for the Point</param>
    public Point(Vector vector) : this(vector.x, vector.y, vector.z, vector.units, vector.applicationId)
    {}

    /// <summary>
    /// The x coordinate of the point.
    /// </summary>
    public double x { get; set; }

    /// <summary>
    /// The y coordinate of the point.
    /// </summary>
    public double y { get; set; }

    /// <summary>
    /// The z coordinate of the point.
    /// </summary>
    public double z { get; set; }

    /// <summary>
    /// The unit's this <see cref="Vector"/> is in.
    /// This should be one of the units specified in <see cref="Speckle.Core.Kits.Units"/>
    /// </summary>
    public string units { get; set; }

    /// <summary>
    /// Returns the coordinates of this <see cref="Point"/> as a list of numbers
    /// </summary>
    /// <returns>A list of coordinates {x, y, z} </returns>
    public List<double> ToList() => new List<double> { x, y, z };

    /// <summary>
    /// Creates a new <see cref="Point"/> based on a list of coordinates and the unit they're drawn in.
    /// </summary>
    /// <param name="list">The list of coordinates {x, y, z}</param>
    /// <param name="units">The units the coordinates are in</param>
    /// <returns>A new <see cref="Point"/> with the provided coordinates.</returns>
    public static Point FromList(IList<double> list, string units) => new Point(list[ 0 ], list[ 1 ], list[ 2 ], units);

    /// <summary>
    /// Deconstructs a <see cref="Point"/> into it's coordinates and units
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    /// <param name="z">The z coordinate</param>
    /// <param name="units">The units the point's coordinates are in.</param>
    public void Deconstruct(out double x, out double y, out double z, out string units)
    {
      Deconstruct(out x, out y, out z);
      units = this.units;
    }

    /// <summary>
    /// Deconstructs a <see cref="Point"/> into it's coordinates and units
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    /// <param name="z">The z coordinate</param>
    public void Deconstruct(out double x, out double y, out double z)
    {
      x = this.x;
      y = this.y;
      z = this.z;
    }

    /// <inheritdoc/>
    public bool TransformTo(Transform transform, out Point point)
    {
      point = transform.ApplyToPoint(this);
      return true;
    }

    
    public static Point operator +(Point point1, Point point2) => new Point(
      point1.x + point2.x,
      point1.y + point2.y,
      point1.z + point2.z, point1.units);

    public static Point operator -(Point point1, Point point2) => new Point(
      point1.x - point2.x,
      point1.y - point2.y,
      point1.z - point2.z, point1.units);

    public static Point operator *(Point point1, Point point2) => new Point(
      point1.x * point2.x,
      point1.y * point2.y,
      point1.z * point2.z, point1.units);

    public static Point operator *(Point point, double val) => new Point(
      point.x * val,
      point.y * val,
      point.z * val, point.units);

    public static Point operator /(Point point, double val) => new Point(
      point.x / val,
      point.y / val,
      point.z / val, point.units);

    public static bool operator ==(Point point1, Point point2)
    {
      if (point1 is null && point2 is null) return true;
      if (point1 is null ^ point2 is null) return false;
      
      return point1.units == point2.units &&
        point1.x == point2.x &&
        point1.y == point2.y &&
        point1.z == point2.z;
    }
    
    public static bool operator !=(Point point1, Point point2) => !(point1 == point2);

    /// <summary>
    /// Computes a point equidistant from two points.
    /// </summary>
    /// <param name="point1">First point.</param>
    /// <param name="point2">Second point.</param>
    /// <returns>A point at the same distance from <paramref name="point1"/> and <paramref name="point2"/></returns>
    public static Point Midpoint(Point point1, Point point2) => new Point(
      0.5 * ( point1.x + point2.x ),
      0.5 * ( point1.y + point2.y ),
      0.5 * ( point1.z + point2.z ), point1.units);
    
    /// <summary>
    /// Computes the distance between two points
    /// </summary>
    /// <param name="point1">First point.</param>
    /// <param name="point2">Second point.</param>
    /// <returns>The distance from <paramref name="point1"/> to <paramref name="point2"/></returns>
    public static double Distance(Point point1, Point point2) => Math.Sqrt(
      Math.Pow(point1.x - point2.x, 2) + Math.Pow(point1.y - point2.y, 2) + Math.Pow(point1.z - point2.z, 2));
    
    /// <inheritdoc/>
    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      var res = TransformTo(transform, out Point pt);
      transformed = pt;
      return res;
    }
  }
}