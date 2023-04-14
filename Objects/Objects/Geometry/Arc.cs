using System;
using System.Collections.Generic;
using Objects.Other;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Geometry;

/// <summary>
/// Represents a sub-curve of a three-dimensional circle.
/// </summary>
public class Arc : Base, IHasBoundingBox, ICurve, IHasArea, ITransformable<Arc>
{
  /// <inheritdoc/>
  public Arc() { }

  /// <summary>
  /// Constructs a new <see cref="Arc"/> using angle values.
  /// </summary>
  /// <param name="plane">The Plane where the arc will be drawn</param>
  /// <param name="radius">The radius of the Arc</param>
  /// <param name="startAngle">The angle formed between the start point and the X Axis of the plane</param>
  /// <param name="endAngle">The angle formed between the end point and the X Axis of the plane</param>
  /// <param name="angleRadians">The total angle of the Arc in Radians</param>
  /// <param name="units">The object's units</param>
  /// <param name="applicationId">The object's unique application ID</param>
  public Arc(
    Plane plane,
    double radius,
    double startAngle,
    double endAngle,
    double angleRadians,
    string units = Units.Meters,
    string applicationId = null
  )
  {
    this.plane = plane;
    this.radius = radius;
    this.startAngle = startAngle;
    this.endAngle = endAngle;
    this.angleRadians = angleRadians;
    this.applicationId = applicationId;
    this.units = units;
  }

  /// <summary>
  /// Initialise an `Arc` using the arc angle and the start and end points.
  /// The radius, midpoint, start angle, and end angle will be calculated.
  /// For now, this assumes 2D arcs on the XY plane
  /// </summary>
  /// <param name="startPoint">The start point of the arc</param>
  /// <param name="endPoint">The end point of the arc</param>
  /// <param name="angleRadians">The arc angle</param>
  /// <param name="units">Units (defaults to "m")</param>
  /// <param name="applicationId">ID given to the arc in the authoring programme (defaults to null)</param>
  public Arc(
    Point startPoint,
    Point endPoint,
    double angleRadians,
    string units = Units.Meters,
    string applicationId = null
  )
    : this(
      new Plane(startPoint, new Vector(0, 0, 1), new Vector(1, 0, 0), new Vector(0, 1, 0), units),
      startPoint,
      endPoint,
      angleRadians,
      units,
      applicationId
    ) { }

  /// <summary>
  /// Initialise an `Arc` using a plane, the arc angle and the start and end points.
  /// The radius, midpoint, start angle, and end angle will be calculated.
  /// </summary>
  /// <param name="plane">The Plane where the arc will be drawn</param>
  /// <param name="startPoint">The start point of the arc</param>
  /// <param name="endPoint">The end point of the arc</param>
  /// <param name="angleRadians">The arc angle</param>
  /// <param name="units">Units (defaults to "m")</param>
  /// <param name="applicationId">ID given to the arc in the authoring programme (defaults to null)</param>
  public Arc(
    Plane plane,
    Point startPoint,
    Point endPoint,
    double angleRadians,
    string units = Units.Meters,
    string applicationId = null
  )
  {
    // don't be annoying
    if (angleRadians > Math.PI * 2)
      throw new SpeckleException("Can't create an arc with an angle greater than 2pi");
    if (startPoint == endPoint)
      throw new SpeckleException("Can't create an arc where the start and end points are the same");

    this.units = units;
    this.startPoint = startPoint;
    this.endPoint = endPoint;
    this.angleRadians = angleRadians;
    this.applicationId = applicationId;

    // find chord and chord angle which may differ from the arc angle
    var chordMidpoint = Point.Midpoint(startPoint, endPoint);
    var chordLength = Point.Distance(startPoint, endPoint);
    var chordAngle = angleRadians;
    if (chordAngle > Math.PI)
      chordAngle -= Math.PI * 2;
    else if (chordAngle < -Math.PI)
      chordAngle += Math.PI * 2;
    // use the law of cosines for an isosceles triangle to get the radius
    radius = chordLength / Math.Sqrt(2 - 2 * Math.Cos(chordAngle));

    // find the chord vector then calculate the perpendicular vector which points to the centre
    // which can be used to find the circle centre point
    var dir = chordAngle < 0 ? -1 : 1;
    var centreToChord = Math.Sqrt(Math.Pow((double)radius, 2) - Math.Pow(chordLength * 0.5, 2));
    var perp = Vector.CrossProduct(new Vector(endPoint - startPoint), plane.normal);
    var circleCentre = chordMidpoint + new Point(perp.Unit() * centreToChord * -dir);
    plane.origin = circleCentre;

    // use the perpendicular vector in the other direction (from the centre to the arc) to find the arc midpoint
    midPoint =
      angleRadians > Math.PI
        ? chordMidpoint + new Point(perp.Unit() * ((double)radius + centreToChord) * -dir)
        : chordMidpoint + new Point(perp.Unit() * ((double)radius - centreToChord) * dir);

    // find the start angle using trig (correcting for quadrant position) and add the arc angle to get the end angle
    startAngle = Math.Tan((startPoint.y - circleCentre.y) / (startPoint.x - circleCentre.x)) % (2 * Math.PI);
    if (startPoint.x > circleCentre.x && startPoint.y < circleCentre.y) // Q4
      startAngle *= -1;
    else if (startPoint.x < circleCentre.x && startPoint.y < circleCentre.y) // Q3
      startAngle += Math.PI;
    else if (startPoint.x < circleCentre.x && startPoint.y > circleCentre.y) // Q2
      startAngle = Math.PI - startAngle;
    endAngle = startAngle + angleRadians;
    // Set the plane of this arc
    this.plane = plane;
  }

  /// <summary>
  /// The radius of the <see cref="Arc"/>
  /// </summary>
  public double? radius { get; set; }

  /// <summary>
  /// The start angle of the <see cref="Arc"/> based on it's <see cref="Arc.plane"/>
  /// </summary>
  public double? startAngle { get; set; }

  /// <summary>
  /// The end angle of the <see cref="Arc"/> based on it's <see cref="Arc.plane"/>
  /// </summary>
  public double? endAngle { get; set; }

  /// <summary>
  /// The inner angle of the <see cref="Arc"/>
  /// </summary>
  public double angleRadians { get; set; }

  /// <summary>
  /// Gets or sets the plane of the <see cref="Arc"/>. The plane origin is the <see cref="Arc"/> center.
  /// </summary>
  public Plane plane { get; set; }

  /// <summary>
  /// The start <see cref="Point"/> of the <see cref="Arc"/>
  /// </summary>
  public Point startPoint { get; set; }

  /// <summary>
  /// Gets or sets the point at 0.5 length.
  /// </summary>
  public Point midPoint { get; set; }

  /// <summary>
  /// The end <see cref="Point"/> of the <see cref="Arc"/>
  /// </summary>
  public Point endPoint { get; set; }

  /// <summary>
  /// The units this object was specified in.
  /// </summary>
  public string units { get; set; }

  /// <inheritdoc/>
  public Interval domain { get; set; }

  /// <inheritdoc/>
  public double length { get; set; }

  /// <inheritdoc/>
  public double area { get; set; }

  /// <inheritdoc/>
  public Box bbox { get; set; }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out Arc transformed)
  {
    startPoint.TransformTo(transform, out Point transformedStartPoint);
    midPoint.TransformTo(transform, out Point transformedMidpoint);
    endPoint.TransformTo(transform, out Point transformedEndPoint);
    plane.TransformTo(transform, out Plane pln);
    var arc = new Arc(pln, transformedStartPoint, transformedEndPoint, angleRadians, units);
    arc.midPoint = transformedMidpoint;
    arc.domain = domain;
    transformed = arc;
    return true;
  }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    var res = TransformTo(transform, out Arc arc);
    transformed = arc;
    return res;
  }

  /// <summary>
  /// Creates a flat list with the values of the <see cref="Arc"/>
  /// This is only used for serialisation purposes.
  /// </summary>
  /// <returns>A list of numbers representing the <see cref="Arc"/>'s value</returns>
  public List<double> ToList()
  {
    var list = new List<double>();
    list.Add(radius ?? 0);
    list.Add(startAngle ?? 0);
    list.Add(endAngle ?? 0);
    list.Add(angleRadians);
    list.Add(domain.start ?? 0);
    list.Add(domain.end ?? 0);

    list.AddRange(plane.ToList());
    list.AddRange(startPoint.ToList());
    list.AddRange(midPoint.ToList());
    list.AddRange(endPoint.ToList());
    list.Add(Units.GetEncodingFromUnit(units));
    list.Insert(0, CurveTypeEncoding.Arc);
    list.Insert(0, list.Count);
    return list;
  }

  /// <summary>
  /// Creates a new <see cref="Arc"/> instance based on a flat list of numerical values.
  /// This is only used for deserialisation purposes.
  /// </summary>
  /// <remarks>The input list should be the result of having called <see cref="Arc.ToList"/></remarks>
  /// <param name="list">A list of numbers</param>
  /// <returns>A new <see cref="Arc"/> with the values assigned from the list.</returns>
  public static Arc FromList(List<double> list)
  {
    var arc = new Arc();

    arc.radius = list[2];
    arc.startAngle = list[3];
    arc.endAngle = list[4];
    arc.angleRadians = list[5];
    arc.domain = new Interval(list[6], list[7]);
    arc.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
    arc.plane = Plane.FromList(list.GetRange(8, 13));
    arc.startPoint = Point.FromList(list.GetRange(21, 3), arc.units);
    arc.midPoint = Point.FromList(list.GetRange(24, 3), arc.units);
    arc.endPoint = Point.FromList(list.GetRange(27, 3), arc.units);
    arc.plane.units = arc.units;

    return arc;
  }
}
