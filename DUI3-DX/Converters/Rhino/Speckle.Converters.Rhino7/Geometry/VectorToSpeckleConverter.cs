using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[Common.NameAndRankValue(nameof(RG.Vector3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Vector3d, SOG.Vector>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public VectorToSpeckleConverter(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Vector3d)target);

  public SOG.Vector RawConvert(RG.Vector3d target) => new(target.X, target.Y, target.Z, Units.Meters);
}
