using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Speckle.Objects.Common;
using Speckle.Objects.Primitives;
using Speckle.Objects.Utils;

namespace Speckle.Objects.Geometry;

/// <summary>
/// A polyline curve, defined by a set of vertices.
/// </summary>
public class Polyline : Base, ICurve, IHasArea, IHasBoundingBox, ITransformable
{
  /// <summary>
  /// Constructs an empty <see cref="Polyline"/>
  /// </summary>
  public Polyline() { }

  /// <summary>
  /// Constructs a new <see cref="Polyline"/> instance from a list of points.
  /// </summary>
  /// <param name="points">The list of points that compose the polyline</param>
  /// <param name="units">The units the coordinates are in.</param>
  /// <param name="applicationId">The unique ID of this polyline in a specific application</param>
  public Polyline(List<Point> points, string units = Units.Meters, string? applicationId = null)
  {
    this.points = points;
    this.units = units;
    this.applicationId = applicationId;
  }

  /// <summary>
  /// Gets or sets the raw coordinates that define this polyline. Use GetPoints instead to access this data as <see cref="Point"/> instances instead.
  /// </summary>
  [DetachProperty, Chunkable(31250)]
  public List<double> value
  {
    get => points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList();
    set => points = GetPoints(value);
  }

  /// <summary>
  /// If true, do not add the last point to the value list. Polyline first and last points should be unique.
  /// </summary>
  public bool closed { get; set; }

  /// <summary>
  /// The unit's this <see cref="Polyline"/> is in.
  /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
  /// </summary>
  public string units { get; set; }

  /// <summary>
  /// Gets the list of points representing the vertices of this polyline.
  /// </summary>
  [JsonIgnore]
  public List<Point> points { get; set; }

  /// <summary>
  /// The internal domain of this curve.
  /// </summary>
  public Interval domain { get; set; } = new(0, 1);

  /// <inheritdoc/>
  public double length { get; set; }

  /// <inheritdoc/>
  public double area { get; set; }

  /// <inheritdoc/>
  public Box bbox { get; set; }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    // transform points
    var transformedPoints = new List<Point>();
    foreach (var point in points)
    {
      point.TransformTo(transform, out Point transformedPoint);
      transformedPoints.Add(transformedPoint);
    }

    transformed = new Polyline
    {
      value = transformedPoints.SelectMany(o => o.ToList()).ToList(),
      closed = closed,
      applicationId = applicationId,
      units = units
    };

    return true;
  }

  ///<remarks>This function may be suboptimal for performance for polylines with many points</remarks>
  /// <returns><see cref="value"/> as List of <see cref="Point"/>s</returns>
  /// <exception cref="SpeckleException">when list is malformed</exception>
  protected List<Point> GetPoints(List<double> array)
  {
    if (array.Count % 3 != 0)
    {
      throw new SpeckleException(
        $"{nameof(Polyline)}.{nameof(array)} list is malformed: expected length to be multiple of 3"
      );
    }

    var pts = new List<Point>(array.Count / 3);
    for (int i = 2; i < array.Count; i += 3)
    {
      pts.Add(new Point(array[i - 2], array[i - 1], array[i], units));
    }

    return pts;
  }

  /// <summary>
  /// Returns the values of this <see cref="Polyline"/> as a list of numbers
  /// </summary>
  /// <returns>A list of values representing the polyline.</returns>
  public List<double> ToList()
  {
    var list = new List<double>();
    list.Add(closed ? 1 : 0); // 2
    list.Add(domain?.start ?? 0); // 3
    list.Add(domain?.end ?? 1); // 4
    list.Add(value.Count); // 5
    list.AddRange(value); // 6 onwards

    list.Add(Units.GetEncodingFromUnit(units));
    list.Insert(0, CurveTypeEncoding.Polyline); // 1
    list.Insert(0, list.Count); // 0
    return list;
  }

  /// <summary>
  /// Creates a new <see cref="Polyline"/> based on a list of coordinates and the unit they're drawn in.
  /// </summary>
  /// <param name="list">The list of values representing this polyline</param>
  /// <returns>A new <see cref="Polyline"/> with the provided values.</returns>

  public static Polyline FromList(List<double> list)
  {
    var polyline = new Polyline { closed = list[2] == 1, domain = new Interval(list[3], list[4]) };
    var pointCount = (int)list[5];
    polyline.value = list.GetRange(6, pointCount);
    polyline.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
    return polyline;
  }
}
