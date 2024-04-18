using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Geometry;
using RasterLayer = ArcGIS.Desktop.Mapping.RasterLayer;
using ArcGIS.Core.Data.Raster;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(RasterLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RasterLayerToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<RasterLayer, SGIS.RasterLayer>
{
  private readonly IRawConversion<Raster, RasterElement> _gisRasterConverter;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public RasterLayerToSpeckleConverter(
    IRawConversion<Raster, RasterElement> gisRasterConverter,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _gisRasterConverter = gisRasterConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target)
  {
    return RawConvert((RasterLayer)target);
  }

  public SGIS.RasterLayer RawConvert(RasterLayer target)
  {
    var speckleLayer = new SGIS.RasterLayer();

    // get document CRS (for writing geometry coords)
    var spatialRef = _contextStack.Current.Document.SpatialReference;
    speckleLayer.crs = new CRS
    {
      wkt = spatialRef.Wkt,
      name = spatialRef.Name,
      units_native = spatialRef.Unit.ToString(),
    };

    // layer native crs (for writing properties e.g. resolution, origin etc.)
    var spatialRefRaster = target.GetSpatialReference();
    // get active map CRS if layer CRS is empty
    if (spatialRefRaster.Unit is null)
    {
      spatialRefRaster = _contextStack.Current.Document.SpatialReference;
    }
    speckleLayer.rasterCrs = new CRS
    {
      wkt = spatialRefRaster.Wkt,
      name = spatialRefRaster.Name,
      units_native = spatialRefRaster.Unit.ToString(),
    };

    // other properties
    speckleLayer.name = target.Name;
    speckleLayer.units = _contextStack.Current.SpeckleUnits;

    // write details about the Raster
    RasterElement element = _gisRasterConverter.RawConvert(target.GetRaster());
    speckleLayer.elements.Add(element);

    return speckleLayer;
  }
}
