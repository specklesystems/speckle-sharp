using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToHostConverter : IRawConversion<IReadOnlyList<Base>, ACG.Geometry>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<List<SOG.Polyline>, ACG.Polyline> _polylineConverter;
  private readonly IRawConversion<List<SOG.Point>, ACG.Multipoint> _multipointConverter;
  private readonly IRawConversion<List<SGIS.GisPolygonGeometry3d>, ACG.Multipatch> _polygon3dConverter;
  private readonly IRawConversion<List<SGIS.GisPolygonGeometry>, ACG.Polygon> _polygonConverter;
  private readonly IRawConversion<List<SGIS.GisMultipatchGeometry>, ACG.Multipatch> _multipatchConverter;

  public GeometryToHostConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<List<SOG.Polyline>, ACG.Polyline> polylineConverter,
    IRawConversion<List<SOG.Point>, ACG.Multipoint> multipointConverter,
    IRawConversion<List<SGIS.GisPolygonGeometry3d>, ACG.Multipatch> polygon3dConverter,
    IRawConversion<List<SGIS.GisPolygonGeometry>, ACG.Polygon> polygonConverter,
    IRawConversion<List<SGIS.GisMultipatchGeometry>, ACG.Multipatch> multipatchConverter
  )
  {
    _contextStack = contextStack;
    _polylineConverter = polylineConverter;
    _multipointConverter = multipointConverter;
    _polygon3dConverter = polygon3dConverter;
    _polygonConverter = polygonConverter;
    _multipatchConverter = multipatchConverter;
  }

  public ACG.Geometry RawConvert(IReadOnlyList<Base> target)
  {
    List<SOG.Point> pointList = new();
    List<SOG.Polyline> polylineList = new();
    List<SGIS.GisPolygonGeometry3d> polygon3dList = new();
    List<SGIS.GisPolygonGeometry> polygonList = new();
    List<SGIS.GisMultipatchGeometry> multipatchList = new();
    foreach (var item in target)
    {
      switch (item)
      {
        case SOG.Point pt:
          pointList.Add(pt);
          continue;
        case SOG.Polyline polyline:
          polylineList.Add(polyline);
          continue;
        case SGIS.GisPolygonGeometry3d polygon3d:
          polygon3dList.Add(polygon3d);
          continue;
        case SGIS.GisPolygonGeometry polygon:
          polygonList.Add(polygon);
          continue;
        case SGIS.GisMultipatchGeometry multipatch:
          multipatchList.Add(multipatch);
          continue;
      }
    }
    try
    {
      foreach (var item in target)
      {
        return item switch
        {
          SOG.Point point => _multipointConverter.RawConvert(pointList),
          SOG.Polyline polyline => _polylineConverter.RawConvert(polylineList),
          SGIS.GisPolygonGeometry3d geometry => _polygon3dConverter.RawConvert(polygon3dList),
          SGIS.GisPolygonGeometry geometry => _polygonConverter.RawConvert(polygonList),
          SGIS.GisMultipatchGeometry mesh => _multipatchConverter.RawConvert(multipatchList),
          _ => throw new NotSupportedException($"No conversion found"),
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
