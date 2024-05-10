using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class IntervalToHostConverter : IRawConversion<SOP.Interval, RG.Interval>
{
  /// <summary>
  /// Converts a Speckle Interval object to a Rhino.Geometry.Interval object.
  /// </summary>
  /// <param name="target">The Speckle Interval to convert.</param>
  /// <returns>The converted Rhino.Geometry.Interval object.</returns>
  /// <exception cref="ArgumentException">Thrown when the start or end value of the Interval is null.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Interval RawConvert(SOP.Interval target)
  {
    if (!target.start.HasValue || !target.end.HasValue) // POC: CNX-9272 Interval start and end being nullable makes no sense.
    {
      throw new ArgumentException("Interval start/end cannot be null");
    }

    return new RG.Interval(target.start.Value, target.end.Value);
  }
}
