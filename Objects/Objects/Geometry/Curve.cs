using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Other;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Geometry;

public class Curve : Base, ICurve, IHasBoundingBox, IHasArea, ITransformable<Curve>, IDisplayValue<Polyline>
{
  /// <summary>
  /// Constructs an empty <see cref="Curve"/> instance.
  /// </summary>
  public Curve() { }

  /// <summary>
  /// Constructs a new <see cref="Curve"/> instance based on displayValue a polyline.
  /// </summary>
  /// <param name="poly">The polyline that will be this curve's <see cref="displayValue"/></param>
  /// <param name="units">The units this curve is be modelled in</param>
  /// <param name="applicationId">The unique ID of this curve in a specific application</param>
  public Curve(Polyline poly, string units = Units.Meters, string applicationId = null)
  {
    displayValue = poly;
    this.applicationId = applicationId;
    this.units = units;
  }

  public int degree { get; set; }

  public bool periodic { get; set; }

  /// <summary>
  /// "True" if weights differ, "False" if weights are the same.
  /// </summary>
  public bool rational { get; set; }

  [DetachProperty, Chunkable(31250)]
  public List<double> points { get; set; }

  /// <summary>
  /// Gets or sets the weights for this <see cref="Curve"/>. Use a default value of 1 for unweighted points.
  /// </summary>
  [DetachProperty, Chunkable(31250)]
  public List<double> weights { get; set; }

  /// <summary>
  /// Gets or sets the knots for this <see cref="Curve"/>. Count should be equal to <see cref="points"/> count + <see cref="degree"/> + 1.
  /// </summary>
  [DetachProperty, Chunkable(31250)]
  public List<double> knots { get; set; }

  public bool closed { get; set; }

  /// <summary>
  /// The units this object was specified in.
  /// </summary>
  public string units { get; set; }

  /// <inheritdoc/>
  public Interval domain { get; set; }

  /// <inheritdoc/>
  public double length { get; set; }

  /// <inheritdoc/>
  [DetachProperty]
  public Polyline displayValue { get; set; }

  /// <inheritdoc/>
  public double area { get; set; }

  /// <inheritdoc/>
  public Box bbox { get; set; }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out Curve transformed)
  {
    // transform points
    var transformedPoints = new List<Point>();
    foreach (var point in GetPoints())
    {
      point.TransformTo(transform, out Point transformedPoint);
      transformedPoints.Add(transformedPoint);
    }

    var result = displayValue.TransformTo(transform, out ITransformable polyline);
    transformed = new Curve
    {
      degree = degree,
      periodic = periodic,
      rational = rational,
      points = transformedPoints.SelectMany(o => o.ToList()).ToList(),
      weights = weights,
      knots = knots,
      displayValue = (Polyline)polyline,
      closed = closed,
      units = units,
      applicationId = applicationId,
      domain = domain != null ? new Interval { start = domain.start, end = domain.end } : null
    };

    return result;
  }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    var res = TransformTo(transform, out Curve curve);
    transformed = curve;
    return res;
  }

  /// <returns><see cref="points"/> as list of <see cref="Point"/>s</returns>
  /// <exception cref="SpeckleException">when list is malformed</exception>
  public List<Point> GetPoints()
  {
    if (points.Count % 3 != 0)
      throw new SpeckleException(
        $"{nameof(Curve)}.{nameof(points)} list is malformed: expected length to be multiple of 3"
      );

    var pts = new List<Point>(points.Count / 3);
    for (int i = 2; i < points.Count; i += 3)
      pts.Add(new Point(points[i - 2], points[i - 1], points[i], units));
    return pts;
  }

  /// <summary>
  /// Returns the vales of this <see cref="Curve"/> as a list of numbers
  /// </summary>
  /// <returns>A list of values representing the <see cref="Curve"/></returns>
  public List<double> ToList()
  {
    var list = new List<double>();
    var curve = this;
    list.Add(curve.degree); // 0
    list.Add(curve.periodic ? 1 : 0); // 1
    list.Add(curve.rational ? 1 : 0); // 2
    list.Add(curve.closed ? 1 : 0); // 3
    list.Add((double)curve.domain.start); // 4
    list.Add((double)curve.domain.end); // 5

    list.Add(curve.points.Count); // 6
    list.Add(curve.weights.Count); // 7
    list.Add(curve.knots.Count); // 8

    list.AddRange(curve.points); // 9 onwards
    list.AddRange(curve.weights);
    list.AddRange(curve.knots);

    list.Add(Units.GetEncodingFromUnit(units));
    list.Insert(0, CurveTypeEncoding.Curve);
    list.Insert(0, list.Count);
    return list;
  }

  /// <summary>
  /// Creates a new <see cref="Curve"/> based on a list of coordinates and the unit they're drawn in.
  /// </summary>
  /// <param name="list">The list of values representing this <see cref="Curve"/></param>
  /// <returns>A new <see cref="Curve"/> with the provided values.</returns>
  public static Curve FromList(List<double> list)
  {
    if (list[0] != list.Count - 1)
      throw new Exception($"Incorrect length. Expected {list[0]}, got {list.Count}.");
    if (list[1] != CurveTypeEncoding.Curve)
      throw new Exception($"Wrong curve type. Expected {CurveTypeEncoding.Curve}, got {list[1]}.");

    var curve = new Curve();
    curve.degree = (int)list[2];
    curve.periodic = list[3] == 1;
    curve.rational = list[4] == 1;
    curve.closed = list[5] == 1;
    curve.domain = new Interval(list[6], list[7]);

    var pointsCount = (int)list[8];
    var weightsCount = (int)list[9];
    var knotsCount = (int)list[10];

    curve.points = list.GetRange(11, pointsCount);
    curve.weights = list.GetRange(11 + pointsCount, weightsCount);
    curve.knots = list.GetRange(11 + pointsCount + weightsCount, knotsCount);

    curve.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
    return curve;
  }
}
