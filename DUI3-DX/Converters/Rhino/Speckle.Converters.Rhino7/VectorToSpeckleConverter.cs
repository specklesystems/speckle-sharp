using Rhino;
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
  private readonly IConversionContext<RhinoDoc, UnitSystem> _conversionContext;

  public VectorToSpeckleConverter(IConversionContext<RhinoDoc, UnitSystem> conversionContext)
  {
    _conversionContext = conversionContext;
  }

  public Base Convert(object target) => RawConvert((Vector3d)target);

  public Objects.Geometry.Vector RawConvert(Vector3d target) =>
    new(target.X, target.Y, target.Z, _conversionContext.SpeckleUnits);
}
