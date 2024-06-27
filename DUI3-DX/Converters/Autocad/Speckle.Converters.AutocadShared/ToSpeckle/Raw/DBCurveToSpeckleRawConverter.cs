using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry.Raw;

public class DBCurveToSpeckleRawConverter : ITypedConverter<ADB.Curve, Objects.ICurve>, ITypedConverter<ADB.Curve, Base>
{
  private readonly ITypedConverter<ADB.Line, SOG.Line> _lineConverter;
  private readonly ITypedConverter<ADB.Polyline, SOG.Autocad.AutocadPolycurve> _polylineConverter;
  private readonly ITypedConverter<ADB.Polyline2d, SOG.Autocad.AutocadPolycurve> _polyline2dConverter;
  private readonly ITypedConverter<ADB.Polyline3d, SOG.Autocad.AutocadPolycurve> _polyline3dConverter;
  private readonly ITypedConverter<ADB.Arc, SOG.Arc> _arcConverter;
  private readonly ITypedConverter<ADB.Circle, SOG.Arc> _circleConverter;
  private readonly ITypedConverter<ADB.Ellipse, SOG.Arc> _ellipseConverter;
  private readonly ITypedConverter<ADB.Spline, SOG.Arc> _splineConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBCurveToSpeckleRawConverter(
    ITypedConverter<ADB.Line, SOG.Line> lineConverter,
    ITypedConverter<ADB.Polyline, SOG.Autocad.AutocadPolycurve> polylineConverter,
    ITypedConverter<ADB.Polyline2d, SOG.Autocad.AutocadPolycurve> polyline2dConverter,
    ITypedConverter<ADB.Polyline3d, SOG.Autocad.AutocadPolycurve> polyline3dConverter,
    ITypedConverter<ADB.Arc, SOG.Arc> arcConverter,
    ITypedConverter<ADB.Circle, SOG.Arc> circleConverter,
    ITypedConverter<ADB.Ellipse, SOG.Arc> ellipseConverter,
    ITypedConverter<ADB.Spline, SOG.Arc> splineConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _polyline2dConverter = polyline2dConverter;
    _polyline3dConverter = polyline3dConverter;
    _arcConverter = arcConverter;
    _circleConverter = circleConverter;
    _ellipseConverter = ellipseConverter;
    _splineConverter = splineConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts an Autocad curve to a Speckle ICurve.
  /// </summary>
  /// <param name="target">The Autocad curve to convert.</param>
  /// <returns>The Speckle curve.</returns>
  /// <remarks>
  /// This is the main converter when the type of curve you input or output does not matter to the caller.<br/>
  /// ⚠️ If an unsupported type of Curve is input, it will be converted as Spline.
  /// </remarks>
  public Objects.ICurve Convert(ADB.Curve target) =>
    target switch
    {
      ADB.Line line => _lineConverter.Convert(line),
      ADB.Polyline polyline => _polylineConverter.Convert(polyline),
      ADB.Polyline2d polyline2d => _polyline2dConverter.Convert(polyline2d),
      ADB.Polyline3d polyline3d => _polyline3dConverter.Convert(polyline3d),
      ADB.Arc arc => _arcConverter.Convert(arc),
      ADB.Circle circle => _circleConverter.Convert(circle),
      ADB.Ellipse ellipse => _ellipseConverter.Convert(ellipse),
      ADB.Spline spline => _splineConverter.Convert(spline),
      _ => _splineConverter.Convert(target.Spline)
    };

  Base ITypedConverter<ADB.Curve, Base>.Convert(ADB.Curve target) => (Base)Convert(target);
}
