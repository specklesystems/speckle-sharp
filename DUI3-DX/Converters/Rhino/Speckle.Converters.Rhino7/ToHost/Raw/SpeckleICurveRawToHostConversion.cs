using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleICurveRawToHostConversion : IRawConversion<ICurve, RG.Curve>
{
  private readonly IRawConversion<SOG.Line, RG.LineCurve> _lineConverter;
  private readonly IRawConversion<SOG.Arc, RG.ArcCurve> _arcConverter;
  private readonly IRawConversion<SOG.Ellipse, RG.NurbsCurve> _ellipseConverter;
  private readonly IRawConversion<SOG.Spiral, RG.PolylineCurve> _spiralConverter;
  private readonly IRawConversion<SOG.Circle, RG.ArcCurve> _circleConverter;
  private readonly IRawConversion<SOG.Polyline, RG.PolylineCurve> _polylineConverter;
  private readonly IRawConversion<SOG.Polycurve, RG.PolyCurve> _polyCurveConverter;
  private readonly IRawConversion<SOG.Curve, RG.NurbsCurve> _nurbsCurveConverter;

  public SpeckleICurveRawToHostConversion(
    IRawConversion<SOG.Line, RG.LineCurve> lineConverter,
    IRawConversion<SOG.Arc, RG.ArcCurve> arcConverter,
    IRawConversion<SOG.Ellipse, RG.NurbsCurve> ellipseConverter,
    IRawConversion<SOG.Spiral, RG.PolylineCurve> spiralConverter,
    IRawConversion<SOG.Circle, RG.ArcCurve> circleConverter,
    IRawConversion<SOG.Polyline, RG.PolylineCurve> polylineConverter,
    IRawConversion<SOG.Polycurve, RG.PolyCurve> polyCurveConverter,
    IRawConversion<SOG.Curve, RG.NurbsCurve> nurbsCurveConverter
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
