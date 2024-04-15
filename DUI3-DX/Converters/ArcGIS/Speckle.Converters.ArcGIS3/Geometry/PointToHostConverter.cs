using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(MapPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostConverter : IRawConversion<SOG.Point, Multipoint>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PointToHostConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Multipoint RawConvert(SOG.Point target)
  {
    MapPoint mapPoint = new MapPointBuilderEx(target.x, target.y, target.z).ToGeometry();
    return new MultipointBuilderEx(mapPoint).ToGeometry();
  }
}
