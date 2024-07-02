using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class IntervalToSpeckleConverter : ITypedConverter<RG.Interval, SOP.Interval>
{
  /// <summary>
  /// Converts a Rhino Interval object to a Speckle Interval object.
  /// </summary>
  /// <param name="target">The Rhino Interval object to be converted.</param>
  /// <returns>The converted Speckle Interval object.</returns>
  public SOP.Interval Convert(RG.Interval target) => new(target.T0, target.T1);
}
