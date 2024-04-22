using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToHostConverter : IRawConversion<Base, ArcGIS.Core.Geometry.Geometry>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;
  private readonly IRawConversion<SOG.Point, ACG.Multipoint> _pointConverter;
  private readonly IRawConversion<GisPolygonGeometry, ACG.Polygon> _polygonConverter;

  public GisFeatureToHostConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter,
    IRawConversion<SOG.Point, ACG.Multipoint> pointConverter,
    IRawConversion<GisPolygonGeometry, ACG.Polygon> polygonConverter
  )
  {
    _contextStack = contextStack;
    _polylineConverter = polylineConverter;
    _pointConverter = pointConverter;
    _polygonConverter = polygonConverter;
  }

  public ACG.Geometry RawConvert(Base target)
  {
    if (target.speckle_type.ToLower().Contains("point"))
    {
      return _pointConverter.RawConvert((SOG.Point)target);
    }
    else if (target.speckle_type.ToLower().Contains("polyline"))
    {
      // POC: TODO
    }
    else if (target.speckle_type.ToLower().Contains("polygon"))
    {
      // POC: TODO
    }
    else
    {
      throw new SpeckleConversionException($"Unknown geometry type {target.speckle_type}");
    }
    throw new SpeckleConversionException($"Conversion of geometry {target} failed");
  }
  // POC: TODO: Add case for NonGeometry Feature (table entry)
  // IF geometry layer, but no geometry found: throw new SpeckleConversionException($"Feature {target} contains no geometry");
}
