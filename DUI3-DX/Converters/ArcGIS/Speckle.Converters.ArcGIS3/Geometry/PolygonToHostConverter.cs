using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(GisPolygonGeometry), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolygonToHostConverter : IRawConversion<GisPolygonGeometry, Polygon>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PolygonToHostConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Polygon RawConvert(GisPolygonGeometry target)
  {
    // To replace with actual geometry
    List<Coordinate2D> newCoordinates =
      new()
      {
        new Coordinate2D(1021570, 1880583),
        new Coordinate2D(1028730, 1880994),
        new Coordinate2D(1029718, 1875644),
        new Coordinate2D(1021405, 1875397)
      };
    return new PolygonBuilderEx(newCoordinates).ToGeometry();
  }
}
