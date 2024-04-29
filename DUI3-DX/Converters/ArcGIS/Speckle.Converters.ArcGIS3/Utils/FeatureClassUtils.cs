using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public class FeatureClassUtils : IFeatureClassUtils
{
  private const string FID_FIELD_NAME = "OBJECTID";

  public object? FieldValueToNativeType(FieldType fieldType, object? value)
  {
    // Geometry: ignored
    // Blob, Raster, TimestampOffset, XML: converted to String (field type already converted to String on Send)
    switch (fieldType)
    {
      case FieldType.GUID:
        return value;
      case FieldType.OID:
        return value;
    }

    if (value is not null)
    {
      try
      {
        switch (fieldType)
        {
          case FieldType.Single:
            return (float)(double)value;
          case FieldType.Integer:
            // need this step because sent "ints" seem to be received as "longs"
            return (int)(long)value;
          case FieldType.BigInteger:
            return (long)value;
          case FieldType.SmallInteger:
            return (short)(long)value;
          case FieldType.Double:
            return (double)value;
        }
      }
      catch (InvalidCastException)
      {
        return value;
      }

      var stringValue = value.ToString();
      if (stringValue is not null)
      {
        try
        {
          switch (fieldType)
          {
            case FieldType.String:
              return stringValue;
            case FieldType.Date:
              return DateTime.Parse(stringValue);
            case FieldType.DateOnly:
              return DateOnly.Parse(stringValue);
            case FieldType.TimeOnly:
              return TimeOnly.Parse(stringValue);
            case FieldType.Blob:
              return stringValue;
            case FieldType.TimestampOffset:
              return stringValue;
            case FieldType.XML:
              return stringValue;
          }
        }
        catch (InvalidCastException)
        {
          return value;
        }
      }
    }

    return value;
  }

  public void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    List<GisFeature> gisFeatures,
    List<FieldDescription> fields,
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter
  )
  {
    // newFeatureClass.DeleteRows(new QueryFilter());
    foreach (GisFeature feat in gisFeatures)
    {
      using (RowBuffer rowBuffer = newFeatureClass.CreateRowBuffer())
      {
        // get attributes
        foreach (FieldDescription field in fields)
        {
          // try to assign values to writeable fields
          if (feat.attributes is not null)
          {
            string key = field.Name;
            FieldType fieldType = field.FieldType;
            var value = feat.attributes[key];
            if (value is not null)
            {
              // POC: get all values in a correct format
              try
              {
                rowBuffer[key] = FieldValueToNativeType(fieldType, value);
              }
              catch (GeodatabaseFeatureException)
              {
                //'The value type is incompatible.'
                // log error!
                rowBuffer[key] = null;
              }
              catch (GeodatabaseGeneralException)
              {
                // field doen't exist: ideally should not be happening
              }
              catch (GeodatabaseFieldException)
              {
                // non-editable Field, do nothing
              }
            }
            else
            {
              rowBuffer[key] = null;
            }
          }
        }

        // get geometries
        if (feat.geometry != null)
        {
          List<Base> geometryToConvert = feat.geometry;
          ACG.Geometry nativeShape = gisGeometryConverter.RawConvert(geometryToConvert);
          rowBuffer[newFeatureClass.GetDefinition().GetShapeField()] = nativeShape;
        }
        // POC: TODO add option for non-geometry features
        newFeatureClass.CreateRow(rowBuffer).Dispose();
        // break;
      }
    }
  }

  public FieldType GetFieldTypeFromInt(int fieldType)
  {
    foreach (FieldType type in Enum.GetValues(typeof(FieldType)))
    {
      if ((int)type == fieldType)
      {
        if (
          type == FieldType.Blob
          || type == FieldType.Raster
          || type == FieldType.XML
          || type == FieldType.TimestampOffset
        )
        {
          return FieldType.String;
        }
        return type;
      }
    }
    throw new GeodatabaseFieldException($"Field type '{fieldType}' is not valid");
  }

  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target)
  {
    List<FieldDescription> fields = new();
    List<string> fieldAdded = new();

    foreach (var field in target.attributes.GetMembers(DynamicBaseMemberType.Dynamic))
    {
      if (!fieldAdded.Contains(field.Key) && field.Key != FID_FIELD_NAME)
      {
        // POC: TODO check for the forbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588
        try
        {
          if (field.Value is not null)
          {
            FieldType fieldType = GetFieldTypeFromInt((int)(long)field.Value);
            fields.Add(new FieldDescription(field.Key, fieldType));
            fieldAdded.Add(field.Key);
          }
          else
          {
            // log missing field
          }
        }
        catch (GeodatabaseFieldException)
        {
          // log missing field
        }
      }
    }
    return fields;
  }

  public ACG.GeometryType GetLayerGeometryType(VectorLayer target)
  {
    string originalGeomType =
      target.geomType != null ? target.geomType : (target.nativeGeomType != null ? target.nativeGeomType : "");
    ACG.GeometryType geomType;

    if (string.IsNullOrEmpty(originalGeomType))
    {
      throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
    }

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

    return geomType;
  }
}
