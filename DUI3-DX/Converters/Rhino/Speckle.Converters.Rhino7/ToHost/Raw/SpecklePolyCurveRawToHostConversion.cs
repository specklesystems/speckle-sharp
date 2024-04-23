using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePolyCurveRawToHostConversion : IRawConversion<SOG.Polycurve, RG.PolyCurve>
{
  public IRawConversion<ICurve, RG.Curve>? CurveConverter { get; set; } // POC: CNX-9311 Circular dependency injected by the container using property.

  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpecklePolyCurveRawToHostConversion(IRawConversion<SOP.Interval, RG.Interval> intervalConverter)
  {
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a SpecklePolyCurve object to a Rhino PolyCurve object.
  /// </summary>
  /// <param name="target">The SpecklePolyCurve object to convert.</param>
  /// <returns>The converted Rhino PolyCurve object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.PolyCurve RawConvert(SOG.Polycurve target)
  {
    RG.PolyCurve result = new();

    foreach (var segment in target.segments)
    {
      var childCurve = CurveConverter!.RawConvert(segment);
      result.Append(childCurve);
    }

    result.Domain = _intervalConverter.RawConvert(target.domain);

    return result;
  }
}
