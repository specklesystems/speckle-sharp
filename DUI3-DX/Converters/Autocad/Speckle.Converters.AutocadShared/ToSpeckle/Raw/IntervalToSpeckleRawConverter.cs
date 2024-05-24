using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class IntervalToSpeckleConverter : ITypedConverter<AG.Interval, SOP.Interval>
{
  public Base Convert(object target) => Convert((AG.Interval)target);

  public SOP.Interval Convert(AG.Interval target) => new(target.LowerBound, target.UpperBound);
}
