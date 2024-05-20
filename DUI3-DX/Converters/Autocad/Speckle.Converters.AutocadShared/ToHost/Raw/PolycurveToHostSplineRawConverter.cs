using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.AutocadShared.ToHost.Raw;

/// <summary>
/// Polycurve segments might appear in different ICurve types which requires to handle separately for each segment.
/// </summary>
public class PolycurveToHostSplineRawConverter : ITypedConverter<SOG.Polycurve, List<ADB.Entity>>
{
  private readonly ITypedConverter<SOG.Line, ADB.Line> _lineConverter;
  private readonly ITypedConverter<SOG.Polyline, ADB.Polyline3d> _polylineConverter;
  private readonly ITypedConverter<SOG.Arc, ADB.Arc> _arcConverter;
  private readonly ITypedConverter<SOG.Curve, ADB.Curve> _curveConverter;

  public PolycurveToHostSplineRawConverter(
    ITypedConverter<SOG.Line, ADB.Line> lineConverter,
    ITypedConverter<SOG.Polyline, ADB.Polyline3d> polylineConverter,
    ITypedConverter<SOG.Arc, ADB.Arc> arcConverter,
    ITypedConverter<SOG.Curve, ADB.Curve> curveConverter
  )
  {
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _arcConverter = arcConverter;
    _curveConverter = curveConverter;
  }

  public List<ADB.Entity> RawConvert(SOG.Polycurve target)
  {
    // POC: We can improve this once we have IIndex of raw converters and we can get rid of case converters?
    // POC: Should we join entities?
    var list = new List<ADB.Entity>();

    foreach (var segment in target.segments)
    {
      switch (segment)
      {
        case SOG.Arc arc:
          list.Add(_arcConverter.RawConvert(arc));
          break;
        case SOG.Line line:
          list.Add(_lineConverter.RawConvert(line));
          break;
        case SOG.Polyline polyline:
          list.Add(_polylineConverter.RawConvert(polyline));
          break;
        case SOG.Curve curve:
          list.Add(_curveConverter.RawConvert(curve));
          break;
        default:
          break;
      }
    }

    return list;
  }
}
