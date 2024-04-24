using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public class FeatureClassUtils : IFeatureClassUtils
{
  public void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    List<GisFeature> gisFeatures,
    List<string> fieldAdded,
    IRawConversion<Base, ACG.Geometry> gisGeometryConverter
  )
  {
    newFeatureClass.DeleteRows(new QueryFilter());
    foreach (GisFeature feat in gisFeatures)
    {
      using (RowBuffer rowBuffer = newFeatureClass.CreateRowBuffer())
      {
        foreach (string field in fieldAdded)
        {
          // try to assign values to writeable fields
          try
          {
            if (feat.attributes is not null)
            {
              var value = feat.attributes[field];
              if (value is not null)
              {
                // POC: get actual value in a correct format
                rowBuffer[field] = value;
              }
              else
              {
                rowBuffer[field] = null;
              }
            }
          }
          catch (GeodatabaseFieldException)
          {
            // non-editable Field, do nothing
          }
          catch (GeodatabaseFeatureException)
          {
            // log error!
            rowBuffer[field] = null;
          }
        }

        if (feat.geometry != null)
        {
          List<Base> geometryToConvert = feat.geometry;
          if (feat.geometry.Count == 0 && feat.displayValue is not null && feat.displayValue.Count > 0)
          {
            geometryToConvert = feat.displayValue;
          }

          foreach (var geometryPart in geometryToConvert)
          {
            // POC: TODO: repeat for all geometries, add as Multipart
            ACG.Geometry nativeShape = gisGeometryConverter.RawConvert(geometryPart);
            rowBuffer[newFeatureClass.GetDefinition().GetShapeField()] = nativeShape;
            break;
          }
        }
        // POC: TODO add option for non-geometry features
        newFeatureClass.CreateRow(rowBuffer).Dispose();
      }
    }
  }

  public FieldType GetFieldTypeFromInt(int fieldType)
  {
    foreach (FieldType type in Enum.GetValues(typeof(FieldType)))
    {
      if ((int)type == fieldType)
      {
        return type;
      }
    }
    throw new GeodatabaseFieldException($"Field type '{fieldType}' is not valid");
  }

  public ACG.GeometryType GetLayerGeometryType(VectorLayer target)
  {
    string originalGeomType = target.geomType;
    ACG.GeometryType geomType;

    if (originalGeomType == null)
    {
      originalGeomType = target.nativeGeomType;
    }
    if (originalGeomType == null)
    {
      throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
    }
    else
    {
      // POC: find better pattern
      if (originalGeomType.ToLower().Contains("none"))
      {
        geomType = ACG.GeometryType.Unknown;
      }
      else if (originalGeomType.ToLower().Contains("pointcloud"))
      {
        geomType = ACG.GeometryType.Unknown;
      }
      else if (originalGeomType.ToLower().Contains("point"))
      {
        geomType = ACG.GeometryType.Multipoint;
      }
      else if (originalGeomType.ToLower().Contains("polyline"))
      {
        geomType = ACG.GeometryType.Polyline;
      }
      else if (originalGeomType.ToLower().Contains("polygon"))
      {
        geomType = ACG.GeometryType.Polygon;
      }
      else if (originalGeomType.ToLower().Contains("multipatch"))
      {
        geomType = ACG.GeometryType.Multipatch;
      }
      else
      {
        throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
      }
    }
    return geomType;
  }
}
