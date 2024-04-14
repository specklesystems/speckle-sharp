using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class IntervalToSpeckleConverter : IRawConversion<RG.Interval, SOP.Interval>
{
  public SOP.Interval RawConvert(RG.Interval target) => new(target.T0, target.T1);
}
