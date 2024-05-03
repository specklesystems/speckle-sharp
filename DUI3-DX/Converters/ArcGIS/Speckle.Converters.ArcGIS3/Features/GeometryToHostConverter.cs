using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToHostConverter : IRawConversion<IReadOnlyList<Base>, ACG.Geometry>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;
  private readonly IRawConversion<List<SOG.Point>, ACG.Multipoint> _multipointConverter;
  private readonly IRawConversion<List<SGIS.GisPolygonGeometry3d>, ACG.Multipatch> _polygon3dConverter;
  private readonly IRawConversion<List<SGIS.GisPolygonGeometry>, ACG.Polygon> _polygonConverter;
  private readonly IRawConversion<List<SGIS.GisMultipatchGeometry>, ACG.Multipatch> _multipatchConverter;

  public GeometryToHostConverter(
    IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter,
    IRawConversion<List<SOG.Point>, ACG.Multipoint> multipointConverter,
    IRawConversion<List<SGIS.GisPolygonGeometry3d>, ACG.Multipatch> polygon3dConverter,
    IRawConversion<List<SGIS.GisPolygonGeometry>, ACG.Polygon> polygonConverter,
    IRawConversion<List<SGIS.GisMultipatchGeometry>, ACG.Multipatch> multipatchConverter
  )
  {
    _polylineConverter = polylineConverter;
    _multipointConverter = multipointConverter;
    _polygon3dConverter = polygon3dConverter;
    _polygonConverter = polygonConverter;
    _multipatchConverter = multipatchConverter;
  }

  public ACG.Geometry RawConvert(IReadOnlyList<Base> target)
  {
    try
    {
      if (target.Count > 0)
      {
        return target[0] switch
        {
          SOG.Point point => _multipointConverter.RawConvert(target.Cast<SOG.Point>().ToList()),
          SOG.Polyline polyline => _polylineConverter.RawConvert(target.Cast<SOG.Polyline>().ToList()[0]),
          SGIS.GisPolygonGeometry3d geometry3d
            => _polygon3dConverter.RawConvert(target.Cast<SGIS.GisPolygonGeometry3d>().ToList()),
          SGIS.GisPolygonGeometry geometry
            => _polygonConverter.RawConvert(target.Cast<SGIS.GisPolygonGeometry>().ToList()),
          SGIS.GisMultipatchGeometry mesh
            => _multipatchConverter.RawConvert(target.Cast<SGIS.GisMultipatchGeometry>().ToList()),
          _ => throw new NotSupportedException($"No conversion found for type {target[0]}"),
        };
      }
      throw new NotSupportedException($"Feature contains no geometry");
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // log errors
    }
  }
}
