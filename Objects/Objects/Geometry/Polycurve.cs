using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Other;

namespace Objects.Geometry
{
  /// <summary>
  /// A curve that is comprised of multiple curves connected.
  /// </summary>
  public class Polycurve : Base, ICurve, IHasArea, IHasBoundingBox, ITransformable
  {
    /// <summary>
    /// Gets or sets the list of segments that comprise this <see cref="Polycurve"/>
    /// </summary>
    public List<ICurve> segments { get; set; } = new List<ICurve>();

    /// <summary>
    /// The internal domain of this curve.
    /// </summary>
    public Interval domain { get; set; }

    /// <summary>
    /// Gets or sets a Boolean value indicating if the <see cref="Polycurve"/> is closed
    /// (i.e. The start point of the first segment and the end point of the last segment coincide.)
    /// </summary>
    public bool closed { get; set; }

    /// <inheritdoc/>
    public Box bbox { get; set; }

    /// <inheritdoc/>
    public double area { get; set; }

    /// <inheritdoc/>
    public double length { get; set; }

    /// <summary>
    /// The unit's this <see cref="Polycurve"/> is in.
    /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
    /// </summary>
    public string units { get; set; }

    /// <summary>
    /// Constructs a new empty <see cref="Polycurve"/> instance.
    /// </summary>
    public Polycurve()
    {
    }

    /// <summary>
    /// Constructs a new empty <see cref="Polycurve"/> with defined units and unique application ID.
    /// </summary>
    /// <param name="units">The units the Polycurve was modelled in.</param>
    /// <param name="applicationId">The unique ID of this polyline in a specific application</param>
    public Polycurve(string units = Units.Meters, string applicationId = null)
    {
      this.applicationId = applicationId;
      this.units = units;
    }

    /// <summary>
    /// Constructs a new <see cref="Polycurve"/> instance from an existing <see cref="Polyline"/> curve.
    /// </summary>
    /// <param name="polyline">The polyline to be used when constructing the <see cref="Polycurve"/></param>
    /// <returns>A <see cref="Polycurve"/> with the same shape as the provided polyline.</returns>
    public static implicit operator Polycurve(Polyline polyline)
    {
      Polycurve polycurve = new Polycurve
      {
        units = polyline.units,
        area = polyline.area,
        domain = polyline.domain,
        closed = polyline.closed,
        bbox = polyline.bbox,
        length = polyline.length
      };

      var points = polyline.GetPoints();
      for (var i = 0; i < points.Count - 1; i++)
      {
        var line = new Line(points[i], points[i + 1], polyline.units);
        polycurve.segments.Add(line);
      }
      if (polyline.closed)
      {
        var line = new Line(points[points.Count - 1], points[0], polyline.units);
        polycurve.segments.Add(line);
      }

      return polycurve;
    }

    /// <summary>
    /// Returns the values of this <see cref="Polycurve"/> as a list of numbers
    /// </summary>
    /// <returns>A list of values representing the polycurve.</returns>
    public List<double> ToList()
    {
      var list = new List<double>();
      list.Add(closed ? 1 : 0);
      list.Add(domain.start ?? 0);
      list.Add(domain.end ?? 1);

      var crvs = CurveArrayEncodingExtensions.ToArray(segments);
      list.Add(crvs.Count);
      list.AddRange(crvs);

      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.PolyCurve);
      list.Insert(0, list.Count);

      return list;
    }

    /// <summary>
    /// Creates a new <see cref="Polycurve"/> based on a list of coordinates and the unit they're drawn in.
    /// </summary>
    /// <param name="list">The list of values representing this polycurve</param>
    /// <returns>A new <see cref="Polycurve"/> with the provided values.</returns>
    public static Polycurve FromList(List<double> list)
    {
      var polycurve = new Polycurve();
      polycurve.closed = list[2] == 1;
      polycurve.domain = new Interval(list[3], list[4]);

      var temp = list.GetRange(6, (int)list[5]);
      polycurve.segments = CurveArrayEncodingExtensions.FromArray(temp);
      polycurve.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
      return polycurve;
    }

    /// <inheritdoc/>
    public bool TransformTo(Transform transform, out ITransformable polycurve)
    {
      // transform segments
      var success = true;
      var transformed = new List<ICurve>();
      foreach (var curve in segments)
      {
        if (curve is ITransformable c)
        {
          c.TransformTo(transform, out ITransformable tc);
          transformed.Add((ICurve)tc);
        }
        else
          success = false;
      }

      polycurve = new Polycurve
      {
        segments = transformed,
        applicationId = applicationId,
        closed = closed,
        units = units
      };

      return success;
    }
  }
}
