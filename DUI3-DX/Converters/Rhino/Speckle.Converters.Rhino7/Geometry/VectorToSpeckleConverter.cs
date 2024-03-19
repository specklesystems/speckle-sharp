using Rhino.Geometry;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(Vector3d), 0)]
public class VectorToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Vector3d, SOG.Vector>
{
  public Base Convert(object target)
  {
    return RawConvert((Vector3d)target);
  }

  public SOG.Vector RawConvert(Vector3d target) => new(target.X, target.Y, target.Z, Units.Meters);
}
