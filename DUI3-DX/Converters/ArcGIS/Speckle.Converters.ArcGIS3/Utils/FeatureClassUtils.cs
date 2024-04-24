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
                rowBuffer[field] = value.ToString();
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

  public ACG.GeometryType GetLayerGeometryType(VectorLayer target)
  {
    ACG.GeometryType geomType;
    if (target.geomType == null)
    {
      throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
    }
    else
    {
      // POC: find better pattern
      if (target.geomType.ToLower().Contains("none"))
      {
        geomType = ACG.GeometryType.Unknown;
      }
      else if (target.geomType.ToLower().Contains("pointcloud"))
      {
        geomType = ACG.GeometryType.Unknown;
      }
      else if (target.geomType.ToLower().Contains("point"))
      {
        geomType = ACG.GeometryType.Multipoint;
      }
      else if (target.geomType.ToLower().Contains("polyline"))
      {
        geomType = ACG.GeometryType.Polyline;
      }
      else if (target.geomType.ToLower().Contains("polygon"))
      {
        geomType = ACG.GeometryType.Polygon;
      }
      else if (target.geomType.ToLower().Contains("multipatch"))
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
