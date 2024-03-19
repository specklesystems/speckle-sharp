using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

// POC: not sure I like the place of the default rank
[NameAndRankValue(nameof(RG.Point3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Point3d, SOG.Point>
{
  public Base Convert(object target) => RawConvert((RG.Point3d)target);

  public SOG.Point RawConvert(RG.Point3d target) => new(target.X, target.Y, target.Z, Units.Meters);
}
