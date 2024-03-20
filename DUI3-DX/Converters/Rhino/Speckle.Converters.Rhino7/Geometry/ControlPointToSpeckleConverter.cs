using Rhino.Geometry.Collections;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.ControlPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class ControlPointToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<RG.ControlPoint, SOG.ControlPoint>
{
  public SOG.ControlPoint RawConvert(RG.ControlPoint target) =>
    new(target.Location.X, target.Location.Y, target.Location.Z, target.Weight, Units.Meters);

  public Base Convert(object target) => RawConvert((RG.ControlPoint)target);
}
