using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using RasterLayer = ArcGIS.Desktop.Mapping.RasterLayer;

namespace Speckle.Converters.ArcGIS3.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RasterLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RasterLayerToSpeckleConverter : IToSpeckleTopLevelConverter, ITypedConverter<RasterLayer, SGIS.RasterLayer>
{
  private readonly ITypedConverter<Raster, RasterElement> _gisRasterConverter;
  private readonly IConversionContextStack<ArcGISDocument, Unit> _contextStack;

  public RasterLayerToSpeckleConverter(
    ITypedConverter<Raster, RasterElement> gisRasterConverter,
    IConversionContextStack<ArcGISDocument, Unit> contextStack
  )
  {
    _gisRasterConverter = gisRasterConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target)
  {
    return Convert((RasterLayer)target);
  }

  public SGIS.RasterLayer Convert(RasterLayer target)
  {
    var speckleLayer = new SGIS.RasterLayer();

    // get document CRS (for writing geometry coords)
    var spatialRef = _contextStack.Current.Document.Map.SpatialReference;
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
      spatialRefRaster = _contextStack.Current.Document.Map.SpatialReference;
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
    RasterElement element = _gisRasterConverter.Convert(target.GetRaster());
    speckleLayer.elements.Add(element);

    return speckleLayer;
  }
}