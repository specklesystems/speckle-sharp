using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleIntervalRawToHostConversion : IRawConversion<SOP.Interval, RG.Interval>
{
  public RG.Interval RawConvert(SOP.Interval target)
  {
    if (!target.start.HasValue || !target.end.HasValue) // POC: I hate interval start/end being nullable. Makes no sense.
    {
      throw new ArgumentException("Interval start/end cannot be null");
    }

    // POC: Interval conversions used to have a unit input, but it was only used in `Box` so we can deal with that on the parent.
    return new RG.Interval(target.start.Value, target.end.Value);
  }
}
