using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data.Raster;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisRasterToSpeckleConverter : ITypedConverter<Raster, RasterElement>
{
  private readonly ITypedConverter<ACG.Geometry, IReadOnlyList<Base>> _geometryConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public GisRasterToSpeckleConverter(
    ITypedConverter<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> geometryConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _geometryConverter = geometryConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => Convert((Raster)target);

  private List<double> GetRasterMeshCoords(Raster target, List<List<byte>> pixelValsPerBand)
  {
    List<byte> pixelsList = pixelValsPerBand[^1];
    var extent = target.GetExtent();
    var cellSize = target.GetMeanCellSize();

    int bandCount = target.GetBandCount();
    float xOrigin = (float)extent.XMin;
    float yOrigin = (float)extent.YMax;
    int xSize = target.GetWidth();
    int ySize = target.GetHeight();
    float xResolution = (float)cellSize.Item1;
    float yResolution = -1 * (float)cellSize.Item2;

    List<double> newCoords = pixelsList
      .SelectMany(
        (_, ind) =>
          new List<double>()
          {
            xOrigin + xResolution * (int)Math.Floor((double)ind / ySize),
            yOrigin + yResolution * (ind % ySize),
            0,
            xOrigin + xResolution * ((int)Math.Floor((double)ind / ySize) + 1),
            yOrigin + yResolution * (ind % ySize),
            0,
            xOrigin + xResolution * (int)Math.Floor((double)ind / ySize + 1),
            yOrigin + yResolution * (ind % ySize + 1),
            0,
            xOrigin + xResolution * (int)Math.Floor((double)ind / ySize),
            yOrigin + yResolution * (ind % ySize + 1),
            0
          }
      )
      .ToList();
    return newCoords;
  }

  private List<int> GetRasterColors(int bandCount, List<List<byte>> pixelValsPerBand)
  {
    List<int> newColors = new();
    List<byte> pixelsList = pixelValsPerBand[^1];
    if (bandCount == 3 || bandCount == 4) // RGB
    {
      var pixMin0 = pixelValsPerBand[0].Min();
      var pixMax0 = pixelValsPerBand[0].Max();
      var pixMin1 = pixelValsPerBand[1].Min();
      var pixMax1 = pixelValsPerBand[1].Max();
      var pixMin2 = pixelValsPerBand[2].Min();
      var pixMax2 = pixelValsPerBand[2].Max();
      newColors = pixelsList
        .Select(
          (_, ind) =>
            (255 << 24)
            | (255 * (pixelValsPerBand[0][ind] - pixMin0) / (pixMax0 - pixMin0) << 16)
            | (255 * (pixelValsPerBand[1][ind] - pixMin1) / (pixMax1 - pixMin1) << 8)
            | 255 * (pixelValsPerBand[2][ind] - pixMin2) / (pixMax2 - pixMin2)
        )
        .SelectMany(x => new List<int>() { x, x, x, x })
        .ToList();
    }
    else // greyscale
    {
      var pixMin = pixelValsPerBand[0].Min();
      var pixMax = pixelValsPerBand[0].Max();
      newColors = pixelsList
        .Select(
          (_, ind) =>
            (255 << 24)
            | (255 * (pixelValsPerBand[0][ind] - pixMin) / (pixMax - pixMin) << 16)
            | (255 * (pixelValsPerBand[0][ind] - pixMin) / (pixMax - pixMin) << 8)
            | 255 * (pixelValsPerBand[0][ind] - pixMin) / (pixMax - pixMin)
        )
        .SelectMany(x => new List<int>() { x, x, x, x })
        .ToList();
    }
    return newColors;
  }

  public RasterElement Convert(Raster target)
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
    float yResolution = -1 * (float)cellSize.Item2;

    var pixelType = target.GetPixelType(); // e.g. UCHAR
    var xyOrigin = target.PixelToMap(0, 0);

    RasterElement rasterElement =
      new(bandCount, new List<string>(), xOrigin, yOrigin, xSize, ySize, xResolution, yResolution, new List<float?>());

    // prepare to construct a mesh
    List<double> newCoords = new();
    List<int> newFaces = new();
    List<int> newColors = new();

    // store band values for renderer
    List<List<byte>> pixelValsPerBand = new();

    for (int i = 0; i < bandCount; i++)
    {
      // Get a pixel block for quicker reading and read from pixel top left pixel
      PixelBlock block = target.CreatePixelBlock(target.GetWidth(), target.GetHeight());
      target.Read(0, 0, block);

      RasterBandDefinition bandDef = target.GetBand(i).GetDefinition();
      string bandName = bandDef.GetName();
      rasterElement.band_names.Add(bandName);

      // Read 2-dimensional pixel values into 1-dimensional byte array
      // TODO: format to list of float
      Array pixels2D = block.GetPixelData(i, false);
      List<byte> pixelsList = pixels2D.Cast<byte>().ToList();
      pixelValsPerBand.Add(pixelsList);

      // transpose to match QGIS data structure
      var transposedPixelList = Enumerable
        .Range(0, ySize)
        .SelectMany((_, ind) => Enumerable.Range(0, xSize).Select(x => pixels2D.GetValue(x, ind)))
        .ToArray();

      rasterElement[$"@(10000){bandName}_values"] = transposedPixelList;

      // null or float for noDataValue
      float? noDataVal = null;
      var noDataValOriginal = bandDef.GetNoDataValue();
      if (noDataValOriginal != null)
      {
        noDataVal = (float)noDataValOriginal;
      }
      rasterElement.noDataValue.Add(noDataVal);

      // construct mesh
      newFaces = pixelsList
        .SelectMany((_, ind) => new List<int>() { 4, 4 * ind, 4 * ind + 1, 4 * ind + 2, 4 * ind + 3 })
        .ToList();

      newCoords = GetRasterMeshCoords(target, pixelValsPerBand);

      // Construct colors only once, when i=last band index
      // ATM, RGB for 3 or 4 bands, greyscale from 1st band for anything else
      if (i == bandCount - 1)
      {
        newColors = GetRasterColors(bandCount, pixelValsPerBand);
      }
    }

    SOG.Mesh mesh = new(newCoords, newFaces, newColors, null, _contextStack.Current.SpeckleUnits, null) { };
    rasterElement.displayValue = new List<SOG.Mesh>() { mesh };

    return rasterElement;
  }
}
