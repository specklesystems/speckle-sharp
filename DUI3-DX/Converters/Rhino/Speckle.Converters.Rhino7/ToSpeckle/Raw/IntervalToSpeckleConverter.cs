using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

public class IntervalToSpeckleConverter : IRawConversion<RG.Interval, SOP.Interval>
{
  public SOP.Interval RawConvert(RG.Interval target) => new(target.T0, target.T1);
}
