using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PolylineToHostConverter
  : ITypedConverter<SOG.Polyline, IRhinoPolyline>,
    ITypedConverter<SOG.Polyline, IRhinoPolylineCurve>
{
  private readonly ITypedConverter<IReadOnlyList<double>, IRhinoPoint3dList> _pointListConverter;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;
  private readonly IRhinoLineFactory _rhinoLineFactory;

  public PolylineToHostConverter(
    ITypedConverter<IReadOnlyList<double>, IRhinoPoint3dList> pointListConverter,
    ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter,
    IRhinoLineFactory rhinoLineFactory
  )
  {
    _pointListConverter = pointListConverter;
    _intervalConverter = intervalConverter;
    _rhinoLineFactory = rhinoLineFactory;
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
  /// <see cref="IRhinoPolylineCurve"/> conversion instead
  /// </remarks>
  public IRhinoPolyline Convert(SOG.Polyline target)
  {
    var points = _pointListConverter.Convert(target.value);

    if (target.closed)
    {
      points.Add(points[0]);
    }

    var poly = _rhinoLineFactory.Create(points);

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
  IRhinoPolylineCurve ITypedConverter<SOG.Polyline, IRhinoPolylineCurve>.Convert(SOG.Polyline target)
  {
    var poly = Convert(target).ToPolylineCurve();
    poly.Domain = _intervalConverter.Convert(target.domain);
    return poly;
  }
}
