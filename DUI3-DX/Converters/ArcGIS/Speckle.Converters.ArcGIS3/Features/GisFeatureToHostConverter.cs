using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToHostConverter : IRawConversion<Base, ArcGIS.Core.Geometry.Geometry>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<SOG.Polyline, Polyline> _polylineConverter;
  private readonly IRawConversion<SOG.Point, Multipoint> _pointConverter;
  private readonly IRawConversion<GisPolygonGeometry, Polygon> _polygonConverter;

  public GisFeatureToHostConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<SOG.Polyline, Polyline> polylineConverter,
    IRawConversion<SOG.Point, Multipoint> pointConverter,
    IRawConversion<GisPolygonGeometry, Polygon> polygonConverter
  )
  {
    _contextStack = contextStack;
    _polylineConverter = polylineConverter;
    _pointConverter = pointConverter;
    _polygonConverter = polygonConverter;
  }

  public ArcGIS.Core.Geometry.Geometry RawConvert(Base target)
  {
    if (target.speckle_type.ToLower().Contains("point"))
    {
      return _pointConverter.RawConvert((SOG.Point)target);
    }
    else if (target.speckle_type.ToLower().Contains("polygon"))
    {
      // return _polygonConverter.RawConvert((GisPolygonGeometry)target.geometry[0]);
    }
    else
    {
      throw new SpeckleConversionException($"Unknown geometry type {target.speckle_type}");
    }
    throw new SpeckleConversionException($"Conversion of geometry {target} failed");
  }
  // Add case for NonGeometry Feature (table entry)
  // throw new SpeckleConversionException($"Feature {target} contains no geometry");
}
