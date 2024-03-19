using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Objects.Primitives;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Interval), 0)]
public class IntervalToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Interval, Interval>
{
  public Base Convert(object target) => RawConvert((RG.Interval)target);

  public Interval RawConvert(RG.Interval target) => new(target.T0, target.T1);
}
