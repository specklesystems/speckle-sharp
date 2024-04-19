using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects.GIS;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(GisPolygonGeometry), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolygonToHostConverter : IRawConversion<GisPolygonGeometry, ACG.Polygon>
{
  public ACG.Polygon RawConvert(GisPolygonGeometry target)
  {
    // TODO: To replace with actual geometry
    List<ACG.Coordinate2D> newCoordinates =
      new()
      {
        new ACG.Coordinate2D(1021570, 1880583),
        new ACG.Coordinate2D(1028730, 1880994),
        new ACG.Coordinate2D(1029718, 1875644),
        new ACG.Coordinate2D(1021405, 1875397)
      };
    return new ACG.PolygonBuilderEx(newCoordinates).ToGeometry();
  }
}
