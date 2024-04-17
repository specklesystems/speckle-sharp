using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data.Raster;
using System.Drawing;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisRasterToSpeckleConverter : IRawConversion<Raster, RasterElement>
{
  private readonly IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> _geometryConverter;

  public GisRasterToSpeckleConverter(
    IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> geometryConverter
  )
  {
    _geometryConverter = geometryConverter;
  }

  public Base Convert(object target) => RawConvert((Raster)target);

  public RasterElement RawConvert(Raster target)
  {
    int bandCount = target.GetBandCount();
    List<string> bandNames = new();
    float xOrigin = new();
    float yOrigin = new();
    int xSize = new();
    int ySize = new();
    float xResolution = new();
    float yResolution = new();
    IReadOnlyList<float> noDataValue = new List<float>();

    return new RasterElement(
    bandCount,
    bandNames,
    xOrigin,
    yOrigin,
    xSize,
    ySize,
    xResolution,
    yResolution,
    noDataValue
    )
  }
}
