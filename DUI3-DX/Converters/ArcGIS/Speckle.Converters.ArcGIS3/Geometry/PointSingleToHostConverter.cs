using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Point, ACG.MapPoint>
{
  public object Convert(Base target) => RawConvert((SOG.Point)target);

  public ACG.MapPoint RawConvert(SOG.Point target)
  {
    return new ACG.MapPointBuilderEx(target.x, target.y, target.z).ToGeometry();
  }
}
