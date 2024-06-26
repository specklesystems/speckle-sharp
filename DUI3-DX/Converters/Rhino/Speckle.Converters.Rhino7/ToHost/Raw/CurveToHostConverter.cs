using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class CurveToHostConverter : ITypedConverter<ICurve, IRhinoCurve>
{
  private readonly ITypedConverter<SOG.Line, IRhinoLineCurve> _lineConverter;
  private readonly ITypedConverter<SOG.Arc, IRhinoArcCurve> _arcConverter;
  private readonly ITypedConverter<SOG.Ellipse, IRhinoNurbsCurve> _ellipseConverter;
  private readonly ITypedConverter<SOG.Spiral, IRhinoPolylineCurve> _spiralConverter;
  private readonly ITypedConverter<SOG.Circle, IRhinoArcCurve> _circleConverter;
  private readonly ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> _polylineConverter;
  private readonly ITypedConverter<SOG.Polycurve, IRhinoPolyCurve> _polyCurveConverter;
  private readonly ITypedConverter<SOG.Curve, IRhinoNurbsCurve> _nurbsCurveConverter;

  public CurveToHostConverter(
    ITypedConverter<SOG.Line, IRhinoLineCurve> lineConverter,
    ITypedConverter<SOG.Arc, IRhinoArcCurve> arcConverter,
    ITypedConverter<SOG.Ellipse, IRhinoNurbsCurve> ellipseConverter,
    ITypedConverter<SOG.Spiral, IRhinoPolylineCurve> spiralConverter,
    ITypedConverter<SOG.Circle, IRhinoArcCurve> circleConverter,
    ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> polylineConverter,
    ITypedConverter<SOG.Polycurve, IRhinoPolyCurve> polyCurveConverter,
    ITypedConverter<SOG.Curve, IRhinoNurbsCurve> nurbsCurveConverter
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
  /// Converts a given ICurve object to an IRhinoCurve object.
  /// </summary>
  /// <param name="target">The ICurve object to convert.</param>
  /// <returns>The converted IRhinoCurve object.</returns>
  /// <exception cref="NotSupportedException">Thrown when the conversion is not supported for the given type of curve.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoCurve Convert(ICurve target) =>
    target switch
    {
      SOG.Line line => _lineConverter.Convert(line),
      SOG.Arc arc => _arcConverter.Convert(arc),
      SOG.Circle circle => _circleConverter.Convert(circle),
      SOG.Ellipse ellipse => _ellipseConverter.Convert(ellipse),
      SOG.Spiral spiral => _spiralConverter.Convert(spiral),
      SOG.Polyline polyline => _polylineConverter.Convert(polyline),
      SOG.Curve curve => _nurbsCurveConverter.Convert(curve),
      SOG.Polycurve polyCurve => _polyCurveConverter.Convert(polyCurve),
      _ => throw new NotSupportedException($"Unable to convert curves of type {target.GetType().Name}")
    };
}
