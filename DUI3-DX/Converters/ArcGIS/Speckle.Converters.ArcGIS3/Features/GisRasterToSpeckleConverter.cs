using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data.Raster;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Geometry;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisRasterToSpeckleConverter : IRawConversion<Raster, RasterElement>
{
  private readonly IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> _geometryConverter;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public GisRasterToSpeckleConverter(
    IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> geometryConverter,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _geometryConverter = geometryConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((Raster)target);

  public RasterElement RawConvert(Raster target)
  {
    // assisting variables
    var extent = target.GetExtent();
    var cellSize = target.GetMeanCellSize();

    // variables to assign
    int bandCount = target.GetBandCount();
    float xOrigin = (float)extent.XMin;
    float yOrigin = (float)extent.YMax;
    int xSize = target.GetWidth();
    int ySize = target.GetHeight();
    float xResolution = (float)cellSize.Item1;
    float yResolution = (float)cellSize.Item2;

    var pixelType = target.GetPixelType(); // e.g. UCHAR
    var xyOrigin = target.PixelToMap(0, 0);

    RasterElement rasterElement =
      new(bandCount, new List<string>(), xOrigin, yOrigin, xSize, ySize, xResolution, yResolution, new List<float?>());

    // prepare to construct a mesh
    List<double> vertices = new();
    List<int> faces = new();

    List<double> newCoords = new();
    List<int> newFaces = new();
    List<int> newColors = new();

    // Get a pixel block for quicker reading and read from pixel top left pixel
    PixelBlock block = target.CreatePixelBlock(target.GetWidth(), target.GetHeight());
    target.Read(0, 0, block);

    for (int i = 0; i < bandCount; i++)
    {
      RasterBandDefinition bandDef = target.GetBand(i).GetDefinition();
      string bandName = bandDef.GetName();
      rasterElement.band_names.Add(bandName);

      // Read 2-dimensional pixel values into 1-dimensional byte array
      Array pixels2D = block.GetPixelData(i, false);
      // TODO: format to list of float
      List<byte> pixelsList = pixels2D.Cast<byte>().ToList();
      rasterElement[$"@(10000){bandName}"] = pixelsList;

      // null or float for noDataValue
      float? noDataVal = null;
      var noDataValOriginal = bandDef.GetNoDataValue();
      if (noDataValOriginal != null)
      {
        noDataVal = (float)noDataValOriginal;
      }
      rasterElement.noDataValue.Add(noDataVal);

      // currently get mesh colors only for the first band
      if (i == 0)
      {
        // int cellsToRender = 10000;
        newFaces = pixelsList
          .SelectMany((_, ind) => new List<int>() { 4, 4 * ind, 4 * ind + 1, 4 * ind + 2, 4 * ind + 3 })
          .ToList();
        //.GetRange(0, 5 * (xSize - 1) * (ySize - 1));
        //.GetRange(0, 5 * cellsToRender);

        newCoords = pixelsList
          .SelectMany(
            (_, ind) =>
              new List<double>()
              {
                xOrigin + xResolution * (ind % ySize),
                yOrigin - yResolution * (int)Math.Floor((double)ind / ySize),
                0,
                xOrigin + xResolution * (ind % ySize),
                yOrigin - yResolution * ((int)Math.Floor((double)ind / ySize) + 1),
                0,
                xOrigin + xResolution * (ind % ySize + 1),
                yOrigin - yResolution * (int)Math.Floor((double)ind / ySize + 1),
                0,
                xOrigin + xResolution * (ind % ySize + 1),
                yOrigin - yResolution * (int)Math.Floor((double)ind / ySize),
                0
              } //.Where((_, ind) => (ind + 1) % xSize != 0 && ind + 1 != ySize)
          )
          .ToList();
        //.GetRange(0, 12 * cellsToRender);

        var pixMin = pixelsList.Min();
        var pixMax = pixelsList.Max();
        newColors = pixelsList
          .Select((_, ind) => 3 * 255 / 100 * (pixelsList[ind] - pixMin) / (pixMax - pixMin))
          .Select((x, ind) => (255 << 24) | (x << 16) | (x << 8) | x)
          .SelectMany(x => new List<int>() { x, x, x, x })
          .ToList();
        //.GetRange(0, 4 * cellsToRender);
      }
    }

    SOG.Mesh mesh = new(newCoords, newFaces, newColors, null, _contextStack.Current.SpeckleUnits, null) { };
    rasterElement.displayValue = new List<SOG.Mesh>() { mesh };

    return rasterElement;
  }
}
