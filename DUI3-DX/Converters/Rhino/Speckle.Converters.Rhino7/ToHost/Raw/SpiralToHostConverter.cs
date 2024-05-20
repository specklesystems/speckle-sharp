using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpiralToHostConverter : ITypedConverter<SOG.Spiral, RG.PolylineCurve>
{
  private readonly ITypedConverter<SOG.Polyline, RG.PolylineCurve> _polylineConverter;
  private readonly ITypedConverter<SOP.Interval, RG.Interval> _intervalConverter;

  public SpiralToHostConverter(
    ITypedConverter<SOG.Polyline, RG.PolylineCurve> polylineConverter,
    ITypedConverter<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _polylineConverter = polylineConverter;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a Speckle Spiral object to a Rhino PolylineCurve object.
  /// </summary>
  /// <param name="target">The Speckle Spiral object to be converted.</param>
  /// <returns>A Rhino PolylineCurve object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.PolylineCurve RawConvert(SOG.Spiral target)
  {
    var result = _polylineConverter.RawConvert(target.displayValue);
    result.Domain = _intervalConverter.RawConvert(target.domain);
    return result;
  }
}
