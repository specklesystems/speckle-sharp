using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePolyCurveToHostConversion : IRawConversion<SOG.Polycurve, RG.PolyCurve>
{
  private readonly IRawConversion<ICurve, RG.Curve> _curveConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpecklePolyCurveToHostConversion(
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter,
    IRawConversion<ICurve, RG.Curve> curveConverter
  )
  {
    _intervalConverter = intervalConverter;
    _curveConverter = curveConverter;
  }

  public RG.PolyCurve RawConvert(SOG.Polycurve target)
  {
    RG.PolyCurve result = new();

    foreach (var segment in target.segments)
    {
      var childCurve = _curveConverter.RawConvert(segment);
      result.Append(childCurve);
    }

    result.Domain = _intervalConverter.RawConvert(target.domain);

    return result;
  }
}
