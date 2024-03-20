using Rhino.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[Common.NameAndRankValue(nameof(Vector3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Vector3d, SOG.Vector>
{
  public Base Convert(object target) => RawConvert((Vector3d)target);

  public SOG.Vector RawConvert(Vector3d target) => new(target.X, target.Y, target.Z, Units.Meters);
}
