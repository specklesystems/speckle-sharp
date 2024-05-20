using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

// POC: CNX-9278 This converter decides which specific curve conversion to use. IIndex may be a better choice.
public class CurveToSpeckleConverter : ITypedConverter<RG.Curve, ICurve>, ITypedConverter<RG.Curve, Base>
{
  private readonly ITypedConverter<RG.PolyCurve, SOG.Polycurve> _polyCurveConverter;
  private readonly ITypedConverter<RG.ArcCurve, ICurve> _arcCurveConverter;
  private readonly ITypedConverter<RG.PolylineCurve, SOG.Polyline> _polylineConverter;
  private readonly ITypedConverter<RG.NurbsCurve, SOG.Curve> _nurbsCurveConverter;
  private readonly ITypedConverter<RG.LineCurve, SOG.Line> _lineCurveConverter;

  public CurveToSpeckleConverter(
    ITypedConverter<RG.PolyCurve, SOG.Polycurve> polyCurveConverter,
    ITypedConverter<RG.ArcCurve, ICurve> arcCurveConverter,
    ITypedConverter<RG.PolylineCurve, SOG.Polyline> polylineConverter,
    ITypedConverter<RG.NurbsCurve, SOG.Curve> nurbsCurveConverter,
    ITypedConverter<RG.LineCurve, SOG.Line> lineCurveConverter
  )
  {
    _polyCurveConverter = polyCurveConverter;
    _arcCurveConverter = arcCurveConverter;
    _polylineConverter = polylineConverter;
    _nurbsCurveConverter = nurbsCurveConverter;
    _lineCurveConverter = lineCurveConverter;
  }

  /// <summary>
  /// Converts a Rhino curve to a Speckle ICurve.
  /// </summary>
  /// <param name="target">The Rhino curve to convert.</param>
  /// <returns>The Speckle curve.</returns>
  /// <remarks>
  /// This is the main converter when the type of curve you input or output does not matter to the caller.<br/>
  /// ⚠️ If an unsupported type of Curve is input, it will be converted to NURBS.
  /// </remarks>
  public ICurve RawConvert(RG.Curve target) =>
    target switch
    {
      RG.PolyCurve polyCurve => _polyCurveConverter.RawConvert(polyCurve),
      RG.ArcCurve arcCurve => _arcCurveConverter.RawConvert(arcCurve),
      RG.PolylineCurve polylineCurve => _polylineConverter.RawConvert(polylineCurve),
      RG.LineCurve lineCurve => _lineCurveConverter.RawConvert(lineCurve),
      _ => _nurbsCurveConverter.RawConvert(target.ToNurbsCurve())
    };

  Base ITypedConverter<RG.Curve, Base>.RawConvert(RG.Curve target) => (Base)RawConvert(target);
}
