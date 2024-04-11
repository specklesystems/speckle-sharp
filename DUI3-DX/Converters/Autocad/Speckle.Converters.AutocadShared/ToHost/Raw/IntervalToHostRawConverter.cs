using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.AutocadShared.ToHost.Raw;
public class IntervalToHostRawConverter : IRawConversion<SOP.Interval, AG.Interval>
{
  public AG.Interval RawConvert(SOP.Interval target) => new((double)target.start, (double)target.end, 0.000); // POC: the tolerance might be in some settings or in some context?
}
