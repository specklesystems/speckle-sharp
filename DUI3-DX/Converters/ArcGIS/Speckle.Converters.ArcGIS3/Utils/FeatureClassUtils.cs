using ArcGIS.Core.Data;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public class FeatureClassUtils : IFeatureClassUtils
{
  private readonly IArcGISFieldUtils _fieldsUtils;

  public FeatureClassUtils(IArcGISFieldUtils fieldsUtils)
  {
    _fieldsUtils = fieldsUtils;
  }

  public void AddFeaturesToTable(Table newFeatureClass, List<GisFeature> gisFeatures, List<FieldDescription> fields)
  {
    foreach (GisFeature feat in gisFeatures)
    {
      using (RowBuffer rowBuffer = newFeatureClass.CreateRowBuffer())
      {
        newFeatureClass.CreateRow(_fieldsUtils.AssignFieldValuesToRow(rowBuffer, fields, feat)).Dispose();
      }
    }
  }

  public void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    List<GisFeature> gisFeatures,
    List<FieldDescription> fields,
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter
  )
  {
    foreach (GisFeature feat in gisFeatures)
    {
      using (RowBuffer rowBuffer = newFeatureClass.CreateRowBuffer())
      {
        if (feat.geometry != null)
        {
          List<Base> geometryToConvert = feat.geometry;
          ACG.Geometry nativeShape = gisGeometryConverter.RawConvert(geometryToConvert);
          rowBuffer[newFeatureClass.GetDefinition().GetShapeField()] = nativeShape;
        }
        else
        {
          throw new SpeckleConversionException("No geomerty to write");
        }

        // get attributes
        newFeatureClass.CreateRow(_fieldsUtils.AssignFieldValuesToRow(rowBuffer, fields, feat)).Dispose();
      }
    }
  }

  public ACG.GeometryType GetLayerGeometryType(VectorLayer target)
  {
    string? originalGeomType = target.geomType != null ? target.geomType : target.nativeGeomType;
    ACG.GeometryType geomType;

    if (string.IsNullOrEmpty(originalGeomType))
    {
      throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
    }

    // POC: find better pattern
    if (originalGeomType.Contains("none", StringComparison.CurrentCultureIgnoreCase))
    {
      geomType = ACG.GeometryType.Unknown;
    }
    else if (originalGeomType.Contains("pointcloud", StringComparison.CurrentCultureIgnoreCase))
    {
      geomType = ACG.GeometryType.Unknown;
    }
    else if (originalGeomType.Contains("point", StringComparison.CurrentCultureIgnoreCase))
    {
      geomType = ACG.GeometryType.Multipoint;
    }
    else if (originalGeomType.Contains("polyline", StringComparison.CurrentCultureIgnoreCase))
    {
      geomType = ACG.GeometryType.Polyline;
    }
    else if (originalGeomType.Contains("polygon", StringComparison.CurrentCultureIgnoreCase))
    {
      geomType = ACG.GeometryType.Polygon;
    }
    else if (originalGeomType.Contains("multipatch", StringComparison.CurrentCultureIgnoreCase))
    {
      geomType = ACG.GeometryType.Multipatch;
    }
    else
    {
      throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
    }

    return geomType;
  }
}
