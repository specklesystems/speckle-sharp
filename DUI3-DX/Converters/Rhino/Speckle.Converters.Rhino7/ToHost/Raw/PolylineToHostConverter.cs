using Rhino.Collections;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PolylineToHostConverter
  : ITypedConverter<SOG.Polyline, RG.Polyline>,
    ITypedConverter<SOG.Polyline, RG.PolylineCurve>
{
  private readonly ITypedConverter<IReadOnlyList<double>, Point3dList> _pointListConverter;
  private readonly ITypedConverter<SOP.Interval, RG.Interval> _intervalConverter;

  public PolylineToHostConverter(
    ITypedConverter<IReadOnlyList<double>, Point3dList> pointListConverter,
    ITypedConverter<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _pointListConverter = pointListConverter;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a Speckle polyline object to a Rhino Polyline object.
  /// </summary>
  /// <param name="target">The Speckle polyline object to be converted.</param>
  /// <returns>The converted Rhino Polyline object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks>
  /// <br/>⚠️ This conversion does not preserve the curve domain.
  /// If you need it preserved you must request a conversion to
  /// <see cref="RG.PolylineCurve"/> conversion instead
  /// </remarks>
  public RG.Polyline Convert(SOG.Polyline target)
  {
    var points = _pointListConverter.Convert(target.value);

    if (target.closed)
    {
      points.Add(points[0]);
    }

    var poly = new RG.Polyline(points);

    return poly;
  }

  // POC: CNX-9271 Potential code-smell by directly implementing the interface. We should discuss this further but
  // since we're using the interfaces instead of the direct type, this may not be an issue.
  /// <summary>
  /// Converts a Speckle polyline object to a Rhino Polyline object.
  /// </summary>
  /// <param name="target">The Speckle polyline object to be converted.</param>
  /// <returns>The converted Rhino Polyline object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  RG.PolylineCurve ITypedConverter<SOG.Polyline, RG.PolylineCurve>.Convert(SOG.Polyline target)
  {
    var poly = Convert(target).ToPolylineCurve();
    poly.Domain = _intervalConverter.Convert(target.domain);
    return poly;
  }
}
