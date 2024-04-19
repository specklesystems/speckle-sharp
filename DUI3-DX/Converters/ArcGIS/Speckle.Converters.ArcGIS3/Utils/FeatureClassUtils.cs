using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public class FeatureClassUtils
{
  public void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    VectorLayer target,
    List<string> fieldAdded,
    IRawConversion<Base, ArcGIS.Core.Geometry.Geometry> gisGeometryConverter
  )
  {
    newFeatureClass.DeleteRows(new QueryFilter());
    foreach (GisFeature feat in target.elements.Cast<GisFeature>())
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
          foreach (var geometryPart in feat.geometry)
          {
            // POC: TODO: repeat for all geometries, add as Multipart
            ArcGIS.Core.Geometry.Geometry nativeShape = gisGeometryConverter.RawConvert(geometryPart);
            rowBuffer[newFeatureClass.GetDefinition().GetShapeField()] = nativeShape;
            break;
          }
        }
        // POC: TODO add option for non-geometry features
        newFeatureClass.CreateRow(rowBuffer).Dispose();
      }
    }
  }

  public GeometryType GetLayerGeometryType(VectorLayer target)
  {
    GeometryType geomType = new();
    if (target.nativeGeomType == null)
    {
      throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
    }
    else
    {
      if (target.nativeGeomType.ToLower().Contains("point"))
      {
        geomType = GeometryType.Multipoint;
      }
      else if (target.nativeGeomType.ToLower().Contains("polyline"))
      {
        geomType = GeometryType.Polyline;
      }
      else if (target.nativeGeomType.ToLower().Contains("polygon"))
      {
        geomType = GeometryType.Polygon;
      }
      else if (target.nativeGeomType.ToLower().Contains("multipatch"))
      {
        geomType = GeometryType.Multipatch;
      }
      // throw
    }
    return geomType;
  }
}
