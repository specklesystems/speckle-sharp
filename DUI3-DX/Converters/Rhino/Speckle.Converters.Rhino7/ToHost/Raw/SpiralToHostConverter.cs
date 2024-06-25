using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpiralToHostConverter : ITypedConverter<SOG.Spiral, IRhinoPolylineCurve>
{
  private readonly ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> _polylineConverter;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;

  public SpiralToHostConverter(
    ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> polylineConverter,
    ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter
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
  public IRhinoPolylineCurve Convert(SOG.Spiral target)
  {
    var result = _polylineConverter.Convert(target.displayValue);
    result.Domain = _intervalConverter.Convert(target.domain);
    return result;
  }
}
