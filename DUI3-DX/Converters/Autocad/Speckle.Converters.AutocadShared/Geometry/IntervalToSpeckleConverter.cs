using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(AG.Interval), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class IntervalToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<AG.Interval, SOP.Interval>
{
  public Base Convert(object target) => RawConvert((AG.Interval)target);

  public SOP.Interval RawConvert(AG.Interval target) => new(target.LowerBound, target.UpperBound);
}
