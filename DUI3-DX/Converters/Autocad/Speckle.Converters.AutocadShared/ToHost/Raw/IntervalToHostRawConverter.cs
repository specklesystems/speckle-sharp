using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.AutocadShared.ToHost.Raw;

public class IntervalToHostRawConverter : IRawConversion<SOP.Interval, AG.Interval>
{
  /// <exception cref="ArgumentNullException"> Throws if target start or end value is null.</exception>
  public AG.Interval RawConvert(SOP.Interval target)
  {
    // POC: the tolerance might be in some settings or in some context?
    if (target.start is null || target.end is null)
    {
      throw new ArgumentNullException(nameof(target), "Cannot convert interval without start or end values.");
    }

    return new((double)target.start, (double)target.end, 0.000);
  }
}
