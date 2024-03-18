using Rhino.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7;

// POC: not sure I like the place of the default rank
[NameAndRankValue(nameof(Vector3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<Vector3d, Objects.Geometry.Vector>
{
  public Base Convert(object target)
  {
    return RawConvert((Vector3d)target);
  }

  public Objects.Geometry.Vector RawConvert(Vector3d target) => new(target.X, target.Y, target.Z, Units.Meters);
}
