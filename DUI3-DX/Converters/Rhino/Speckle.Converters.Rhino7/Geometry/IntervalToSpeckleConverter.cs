using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Objects.Primitives;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Interval), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class IntervalToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Interval, Interval>
{
  public Base Convert(object target) => RawConvert((RG.Interval)target);

  public Interval RawConvert(RG.Interval target) => new(target.T0, target.T1);
}
