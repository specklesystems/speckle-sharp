using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class CurveToHostConverter : ITypedConverter<ICurve, RG.Curve>
{
  private readonly ITypedConverter<SOG.Line, RG.LineCurve> _lineConverter;
  private readonly ITypedConverter<SOG.Arc, RG.ArcCurve> _arcConverter;
  private readonly ITypedConverter<SOG.Ellipse, RG.NurbsCurve> _ellipseConverter;
  private readonly ITypedConverter<SOG.Spiral, RG.PolylineCurve> _spiralConverter;
  private readonly ITypedConverter<SOG.Circle, RG.ArcCurve> _circleConverter;
  private readonly ITypedConverter<SOG.Polyline, RG.PolylineCurve> _polylineConverter;
  private readonly ITypedConverter<SOG.Polycurve, RG.PolyCurve> _polyCurveConverter;
  private readonly ITypedConverter<SOG.Curve, RG.NurbsCurve> _nurbsCurveConverter;

  public CurveToHostConverter(
    ITypedConverter<SOG.Line, RG.LineCurve> lineConverter,
    ITypedConverter<SOG.Arc, RG.ArcCurve> arcConverter,
    ITypedConverter<SOG.Ellipse, RG.NurbsCurve> ellipseConverter,
    ITypedConverter<SOG.Spiral, RG.PolylineCurve> spiralConverter,
    ITypedConverter<SOG.Circle, RG.ArcCurve> circleConverter,
    ITypedConverter<SOG.Polyline, RG.PolylineCurve> polylineConverter,
    ITypedConverter<SOG.Polycurve, RG.PolyCurve> polyCurveConverter,
    ITypedConverter<SOG.Curve, RG.NurbsCurve> nurbsCurveConverter
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _ellipseConverter = ellipseConverter;
    _spiralConverter = spiralConverter;
    _circleConverter = circleConverter;
    _polylineConverter = polylineConverter;
    _polyCurveConverter = polyCurveConverter;
    _nurbsCurveConverter = nurbsCurveConverter;
  }

  /// <summary>
  /// Converts a given ICurve object to an RG.Curve object.
  /// </summary>
  /// <param name="target">The ICurve object to convert.</param>
  /// <returns>The converted RG.Curve object.</returns>
  /// <exception cref="NotSupportedException">Thrown when the conversion is not supported for the given type of curve.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Curve RawConvert(ICurve target) =>
    target switch
    {
      SOG.Line line => _lineConverter.RawConvert(line),
      SOG.Arc arc => _arcConverter.RawConvert(arc),
      SOG.Circle circle => _circleConverter.RawConvert(circle),
      SOG.Ellipse ellipse => _ellipseConverter.RawConvert(ellipse),
      SOG.Spiral spiral => _spiralConverter.RawConvert(spiral),
      SOG.Polyline polyline => _polylineConverter.RawConvert(polyline),
      SOG.Curve curve => _nurbsCurveConverter.RawConvert(curve),
      SOG.Polycurve polyCurve => _polyCurveConverter.RawConvert(polyCurve),
      _ => throw new NotSupportedException($"Unable to convert curves of type {target.GetType().Name}")
    };
}
