using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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

    QueuedTask.Run(() =>
    {
      // maybe unnecessary
      var rasterTbl = target.GetAttributeTable();
      if (rasterTbl != null)
      {
        var cursor = rasterTbl.Search();
        while (cursor.MoveNext())
        {
          var row = cursor.Current;
        }
      }
    });

    RasterElement rasterElement =
      new(bandCount, new List<string>(), xOrigin, yOrigin, xSize, ySize, xResolution, yResolution, new List<float?>());

    // prepare to construct a mesh
    List<double> vertices = new();
    List<int> faces = new();

    List<double> newCoords = new();
    List<int> newFaces = new();

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

      //
      if (i == 0) // band index
      {
        newFaces = pixelsList.SelectMany((_, ind) => new List<int>() { 3, ind, ind + 1, ind + 2 }).ToList(); //.Select(s => s + n)) ;
        newCoords = pixelsList
          .SelectMany(
            (_, ind) =>
              new List<double>()
              {
                xOrigin + xResolution * (ind % xSize - 1),
                yOrigin - yResolution * (int)Math.Floor((double)ind / xSize),
                0,
                xOrigin + xResolution * (ind % xSize),
                yOrigin - yResolution * ((int)Math.Floor((double)ind / xSize) + 1),
                0,
                xOrigin + xResolution * (ind % xSize + 1),
                yOrigin - yResolution * ((int)Math.Floor((double)ind / xSize) + 2),
                0,
                xOrigin + xResolution * (ind % xSize + 2),
                yOrigin - yResolution * ((int)Math.Floor((double)ind / xSize) + 3),
                0
              }
          )
          .ToList();
        rasterElement["@(10000)weird_field"] = newFaces;
        rasterElement["@(10000)weird_field2"] = newCoords;
        //x => new List<byte>() { 4, x. });
      }
    }

    SOG.Mesh mesh = new(newCoords, newFaces, null, null, _contextStack.Current.SpeckleUnits, null) { };
    rasterElement.displayValue = new List<SOG.Mesh>() { mesh };

    return rasterElement;
  }
}
