using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Data.Analyst3D;
using ArcGIS.Core.CIM;
using Speckle.Converters.ArcGIS3.Geometry;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(LasDatasetLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointCloudToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<LasDatasetLayer, SGIS.VectorLayer>
{
  private readonly IRawConversion<MapPoint, SOG.Point> _pointConverter;
  private readonly IRawConversion<Envelope, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PointCloudToSpeckleConverter(
    IRawConversion<MapPoint, SOG.Point> pointConverter,
    IRawConversion<Envelope, SOG.Box> boxConverter,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target)
  {
    return RawConvert((LasDatasetLayer)target);
  }

  public SGIS.VectorLayer RawConvert(LasDatasetLayer target)
  {
    SGIS.VectorLayer speckleLayer = new();

    // get document CRS (for writing geometry coords)
    var spatialRef = _contextStack.Current.Document.SpatialReference;
    speckleLayer.crs = new SGIS.CRS
    {
      wkt = spatialRef.Wkt,
      name = spatialRef.Name,
      units_native = spatialRef.Unit.ToString(),
    };

    // other properties
    speckleLayer.name = target.Name;
    speckleLayer.units = _contextStack.Current.SpeckleUnits;
    speckleLayer.nativeGeomType = target.MapLayerType.ToString();
    speckleLayer.geomType = "Pointcloud";

    // prepare data for pointcloud
    List<SOG.Point> specklePts = new();
    List<double> values = new();
    List<int> speckleColors = new();
    var renderer = target.GetRenderers()[0];

    using (LasPointCursor ptCursor = target.SearchPoints(new ArcGIS.Core.Data.Analyst3D.LasPointFilter()))
    {
      while (ptCursor.MoveNext())
      {
        using (LasPoint pt = ptCursor.Current)
        {
          specklePts.Add(_pointConverter.RawConvert(pt.ToMapPoint()));
          values.Add(pt.ClassCode);

          // get color
          int color = 0;
          string classCode = pt.ClassCode.ToString();
          if (renderer is CIMTinUniqueValueRenderer uniqueRenderer)
          {
            foreach (CIMUniqueValueGroup group in uniqueRenderer.Groups)
            {
              if (color != 0)
              {
                break;
              }
              foreach (CIMUniqueValueClass groupClass in group.Classes)
              {
                if (color != 0)
                {
                  break;
                }
                for (int i = 0; i < groupClass.Values.Length; i++)
                {
                  if (classCode == groupClass.Values[i].FieldValues[0])
                  {
                    CIMColor symbolColor = groupClass.Symbol.Symbol.GetColor();
                    color = symbolColor.CIMColorToInt();
                    speckleColors.Add(color);
                    break;
                  }
                }
              }
            }
          }
          else
          {
            color = pt.RGBColor.RGBToInt();
            speckleColors.Add(color);
          }
          //
        }
      }
    }

    SOG.Pointcloud cloud =
      new()
      {
        points = specklePts.SelectMany(pt => new List<double>() { pt.x, pt.y, pt.z }).ToList(),
        colors = speckleColors,
        sizes = values,
        bbox = _boxConverter.RawConvert(target.QueryExtent()),
        units = _contextStack.Current.SpeckleUnits
      };

    speckleLayer.elements.Add(cloud);
    return speckleLayer;
  }
}
