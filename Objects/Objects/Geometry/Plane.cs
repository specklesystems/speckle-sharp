using System.Collections.Generic;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry;

/// <summary>
/// A 3-dimensional Plane consisting of an origin <see cref="Point"/>, and 3 <see cref="Vector"/> as it's X, Y and Z axis.
/// </summary>
public class Plane : Base, ITransformable<Plane>
{
  /// <summary>
  /// Constructs an empty <see cref="Plane"/>
  /// </summary>
  public Plane() { }

  /// <summary>
  /// Constructs a new <see cref="Plane"/> given it's individual values.
  /// </summary>
  /// <param name="origin">The point to be used as origin</param>
  /// <param name="normal">The vector to be used as Z axis</param>
  /// <param name="xDir">The vector to be used as the X axis</param>
  /// <param name="yDir">The vector to be used as the Y axis</param>
  /// <param name="units">The units the coordinates are in.</param>
  /// <param name="applicationId">The unique ID of this polyline in a specific application</param>
  public Plane(
    Point origin,
    Vector normal,
    Vector xDir,
    Vector yDir,
    string units = Units.Meters,
    string? applicationId = null
  )
  {
    this.origin = origin;
    this.normal = normal;
    xdir = xDir;
    ydir = yDir;
    this.applicationId = applicationId;
    this.units = units;
  }

  /// <summary>
  /// The <see cref="Plane"/>s origin point.
  /// </summary>
  public Point origin { get; set; }

  /// <summary>
  /// The <see cref="Plane"/>s Z axis.
  /// </summary>
  public Vector normal { get; set; }

  /// <summary>
  /// The <see cref="Plane"/>s X axis.
  /// </summary>
  public Vector xdir { get; set; }

  /// <summary>
  /// The <see cref="Plane"/>s Y axis.
  /// </summary>
  public Vector ydir { get; set; }

  /// <summary>
  /// The unit's this <see cref="Plane"/> is in.
  /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
  /// </summary>
  public string units { get; set; }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out Plane transformed)
  {
    origin.TransformTo(transform, out Point transformedOrigin);
    normal.TransformTo(transform, out Vector transformedNormal);
    xdir.TransformTo(transform, out Vector transformedXdir);
    ydir.TransformTo(transform, out Vector transformedYdir);
    transformed = new Plane
    {
      origin = transformedOrigin,
      normal = transformedNormal,
      xdir = transformedXdir,
      ydir = transformedYdir,
      applicationId = applicationId,
      units = units
    };

    return true;
  }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    var res = TransformTo(transform, out Plane plane);
    transformed = plane;
    return res;
  }

  /// <summary>
  /// Returns the values of this <see cref="Plane"/> as a list of numbers
  /// </summary>
  /// <returns>A list of values representing the Plane.</returns>

  public List<double> ToList()
  {
    var list = new List<double>();

    list.AddRange(origin.ToList());
    list.AddRange(normal.ToList());
    list.AddRange(xdir.ToList());
    list.AddRange(ydir.ToList());
    list.Add(Units.GetEncodingFromUnit(units));

    return list;
  }

  /// <summary>
  /// Creates a new <see cref="Plane"/> based on a list of values and the unit they're drawn in.
  /// </summary>
  /// <param name="list">The list of values representing this plane</param>
  /// <returns>A new <see cref="Plane"/> with the provided values.</returns>
  public static Plane FromList(List<double> list)
  {
    var plane = new Plane();

    var units = Units.GetUnitFromEncoding(list[list.Count - 1]);
    plane.origin = new Point(list[0], list[1], list[2], units);
    plane.normal = new Vector(list[3], list[4], list[5], units);
    plane.xdir = new Vector(list[6], list[7], list[8], units);
    plane.ydir = new Vector(list[9], list[10], list[11], units);

    return plane;
  }
}
