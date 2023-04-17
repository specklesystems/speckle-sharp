using System.Collections.Generic;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry;

public class Ellipse : Base, ICurve, IHasArea
{
  /// <summary>
  /// Initializes a new instance of the <see cref="Ellipse"/> class.
  /// This constructor is only intended for serialization/deserialization purposes.
  /// Use other constructors to manually create ellipses.
  /// </summary>
  public Ellipse() { }

  /// <summary>
  /// Initializes a new instance of the <see cref="Ellipse"/> class.
  /// </summary>
  /// <param name="plane">The plane to draw the ellipse in.</param>
  /// <param name="radius1">First radius of the ellipse.</param>
  /// <param name="radius2">Second radius of the ellipse.</param>
  /// <param name="applicationId">Application ID, defaults to null.</param>
  public Ellipse(Plane plane, double radius1, double radius2, string units = Units.Meters, string applicationId = null)
    : this(plane, radius1, radius2, new Interval(0, 1), null, units) { }

  /// <summary>
  /// Initializes a new instance of the <see cref="Ellipse"/> class.
  /// </summary>
  /// <param name="plane">The plane to draw the ellipse in.</param>
  /// <param name="radius1">First radius of the ellipse.</param>
  /// <param name="radius2">Second radius of the ellipse.</param>
  /// <param name="domain">The curve's internal parametrization domain.</param>
  /// <param name="trimDomain">The domain to trim the curve with. Will be null if the ellipse is not trimmed.</param>
  /// <param name="applicationId">Application ID, defaults to null.</param>
  public Ellipse(
    Plane plane,
    double radius1,
    double radius2,
    Interval domain,
    Interval trimDomain,
    string units = Units.Meters,
    string applicationId = null
  )
  {
    this.plane = plane;
    firstRadius = radius1;
    secondRadius = radius2;
    this.domain = domain;
    this.trimDomain = trimDomain;
    this.applicationId = applicationId;
    this.units = units;
  }

  /// <summary>
  /// Gets or sets the first radius of the <see cref="Ellipse"/>. This is usually the major radius.
  /// </summary>
  public double? firstRadius { get; set; }

  /// <summary>
  /// Gets or sets the second radius of the <see cref="Ellipse"/>. This is usually the minor radius.
  /// </summary>
  public double? secondRadius { get; set; }

  /// <summary>
  /// Gets or sets the plane to draw this ellipse in.
  /// </summary>
  public Plane plane { get; set; }

  /// <summary>
  /// Gets or set the domain interval to trim this <see cref="Ellipse"/> with.
  /// </summary>
  public Interval trimDomain { get; set; }

  /// <inheritdoc />
  public Box bbox { get; set; }

  public string units { get; set; }

  /// <summary>
  /// Gets or sets the domain interval for this <see cref="Ellipse"/>.
  /// </summary>
  public Interval domain { get; set; }

  /// <inheritdoc />
  public double length { get; set; }

  /// <inheritdoc />
  //public Point center { get; set; }

  /// <inheritdoc />
  public double area { get; set; }

  public List<double> ToList()
  {
    var list = new List<double>();
    list.Add(firstRadius ?? 0);
    list.Add(secondRadius ?? 0);
    list.Add(domain.start ?? 0);
    list.Add(domain.end ?? 0);

    list.AddRange(plane.ToList());

    list.Add(Units.GetEncodingFromUnit(units));
    list.Insert(0, CurveTypeEncoding.Ellipse);
    list.Insert(0, list.Count);
    return list;
  }

  public static Ellipse FromList(List<double> list)
  {
    var ellipse = new Ellipse();

    ellipse.firstRadius = list[2];
    ellipse.secondRadius = list[3];
    ellipse.domain = new Interval(list[4], list[5]);

    ellipse.plane = Plane.FromList(list.GetRange(6, 13));
    ellipse.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
    return ellipse;
  }
}
