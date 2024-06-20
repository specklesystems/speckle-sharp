using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

// POC: CNX-9278 This converter decides which specific curve conversion to use. IIndex may be a better choice.
public class CurveToSpeckleConverter : ITypedConverter<IRhinoCurve, ICurve>, ITypedConverter<IRhinoCurve, Base>
{
  private readonly ITypedConverter<IRhinoPolyCurve, SOG.Polycurve> _polyCurveConverter;
  private readonly ITypedConverter<IRhinoArcCurve, ICurve> _arcCurveConverter;
  private readonly ITypedConverter<IRhinoPolylineCurve, SOG.Polyline> _polylineConverter;
  private readonly ITypedConverter<IRhinoNurbsCurve, SOG.Curve> _nurbsCurveConverter;
  private readonly ITypedConverter<IRhinoLineCurve, SOG.Line> _lineCurveConverter;

  public CurveToSpeckleConverter(
    ITypedConverter<IRhinoPolyCurve, SOG.Polycurve> polyCurveConverter,
    ITypedConverter<IRhinoArcCurve, ICurve> arcCurveConverter,
    ITypedConverter<IRhinoPolylineCurve, SOG.Polyline> polylineConverter,
    ITypedConverter<IRhinoNurbsCurve, SOG.Curve> nurbsCurveConverter,
    ITypedConverter<IRhinoLineCurve, SOG.Line> lineCurveConverter
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
  public ICurve Convert(IRhinoCurve target)
  {
    var polyline = target.ToPolylineCurve();
    if (polyline is not null)
    {
      return _polylineConverter.Convert(polyline);
    }
    var arcCurve = target.ToArcCurve();
    if (arcCurve is not null)
    {
      return _arcCurveConverter.Convert(arcCurve);
    }
    var polyCurve = target.ToPolyCurve();
    if (polyCurve is not null)
    {
      return _polyCurveConverter.Convert(polyCurve);
    }
    var lineCurve = target.ToLineCurve();
    if (lineCurve is not null)
    {
      return _lineCurveConverter.Convert(lineCurve);
    }
    var nurbsCurve = target.ToNurbsCurve();
    return _nurbsCurveConverter.Convert(nurbsCurve);
  }

  Base ITypedConverter<IRhinoCurve, Base>.Convert(IRhinoCurve target) => (Base)Convert(target);
}
