using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.ToHost.Raw;

public class CurveToHostConverter : ITypedConverter<ICurve, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Line, ACG.Polyline> _lineConverter;
  private readonly ITypedConverter<SOG.Arc, ACG.Polyline> _arcConverter;
  private readonly ITypedConverter<SOG.Ellipse, ACG.Polyline> _ellipseConverter;
  private readonly ITypedConverter<SOG.Circle, ACG.Polyline> _circleConverter;
  private readonly ITypedConverter<SOG.Polyline, ACG.Polyline> _polylineConverter;
  private readonly ITypedConverter<SOG.Polycurve, ACG.Polyline> _polyCurveConverter;

  public CurveToHostConverter(
    ITypedConverter<SOG.Line, ACG.Polyline> lineConverter,
    ITypedConverter<SOG.Arc, ACG.Polyline> arcConverter,
    ITypedConverter<SOG.Ellipse, ACG.Polyline> ellipseConverter,
    ITypedConverter<SOG.Circle, ACG.Polyline> circleConverter,
    ITypedConverter<SOG.Polyline, ACG.Polyline> polylineConverter,
    ITypedConverter<SOG.Polycurve, ACG.Polyline> polyCurveConverter
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _ellipseConverter = ellipseConverter;
    _circleConverter = circleConverter;
    _polylineConverter = polylineConverter;
    _polyCurveConverter = polyCurveConverter;
  }

  /// <summary>
  /// Converts a given ICurve object to an ACG.Polyline object.
  /// </summary>
  /// <param name="target">The ICurve object to convert.</param>
  /// <returns>The converted RG.Curve object.</returns>
  /// <exception cref="NotSupportedException">Thrown when the conversion is not supported for the given type of curve.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public ACG.Polyline Convert(ICurve target) =>
    target switch
    {
      SOG.Line line => _lineConverter.Convert(line),
      SOG.Arc arc => _arcConverter.Convert(arc),
      SOG.Circle circle => _circleConverter.Convert(circle),
      SOG.Ellipse ellipse => _ellipseConverter.Convert(ellipse),
      SOG.Spiral spiral => _polylineConverter.Convert(spiral.displayValue),
      SOG.Polyline polyline => _polylineConverter.Convert(polyline),
      SOG.Curve curve => _polylineConverter.Convert(curve.displayValue),
      SOG.Polycurve polyCurve => _polyCurveConverter.Convert(polyCurve),
      _ => throw new NotSupportedException($"Unable to convert curves of type {target.GetType().Name}")
    };
}
