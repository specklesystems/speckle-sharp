using System.Collections.Generic;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry
{
  /// <summary>
  /// Represents a circular curve based on a base <see cref="Plane"/> and a <see cref="double"/> as radius.
  /// </summary>
  public class Circle : Base, ICurve, IHasArea, IHasBoundingBox
  {
    /// <summary>
    /// The radius of the circle
    /// </summary>
    public double? radius { get; set; }

    /// <summary>
    /// The <see cref="Plane"/> the circle lies in.
    /// </summary>
    public Plane plane { get; set; }

    /// <inheritdoc/>
    public Interval domain { get; set; }

    /// <inheritdoc/>
    public Box bbox { get; set; }

    //public Point center { get; set; }

    /// <inheritdoc/>
    public double area { get; set; }

    /// <inheritdoc/>
    public double length { get; set; }

    /// <summary>
    /// The units this object was modeled in.
    /// </summary>
    public string units { get; set; }

    /// <summary>
    /// Constructs an empty <see cref="Circle"/> instance.
    /// </summary>
    public Circle()
    {
    }

    /// <summary>
    /// Constructs a new <see cref="Circle"/> instance.
    /// </summary>
    /// <param name="plane">The plane where the circle lies</param>
    /// <param name="radius">The radius of the circle</param>
    /// <param name="units">The units the circle is modeled in</param>
    /// <param name="applicationId">The unique ID of this circle in a specific application</param>
    public Circle(Plane plane, double radius, string units = Units.Meters, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.applicationId = applicationId;
      this.units = units;
    }

    /// <summary>
    /// Returns the coordinates of this <see cref="Circle"/> as a list of numbers
    /// </summary>
    /// <returns>A list of values representing the <see cref="Circle"/></returns>
    public List<double> ToList()
    {
      var list = new List<double>();

      list.Add(radius ?? 0);
      list.Add(domain.start ?? 0);
      list.Add(domain.end ?? 1);
      list.AddRange(plane.ToList());

      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.Circle);
      list.Insert(0, list.Count);
      return list;
    }

    /// <summary>
    /// Creates a new <see cref="Circle"/> based on a list of coordinates and the unit they're drawn in.
    /// </summary>
    /// <param name="list">The list of values representing this <see cref="Circle"/></param>
    /// <returns>A new <see cref="Circle"/> with the provided values.</returns>
    public static Circle FromList(List<double> list)
    {
      var circle = new Circle();
      circle.radius = list[2];
      circle.domain = new Interval(list[3], list[4]);
      circle.plane = Plane.FromList(list.GetRange(5, 13));
      circle.units = Units.GetUnitFromEncoding(list[list.Count - 1]);

      return circle;
    }
  }
}
