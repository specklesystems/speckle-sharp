using System.Text.RegularExpressions;
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

  public RowBuffer AssignFieldValuesToRow(RowBuffer rowBuffer, List<FieldDescription> fields, GisFeature feat)
  {
    foreach (FieldDescription field in fields)
    {
      // try to assign values to writeable fields
      if (feat.attributes is not null)
      {
        string key = field.AliasName; // use Alias, as Name is simplified to alphanumeric
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
    return rowBuffer;
  }

  public void AddFeaturesToTable(Table newFeatureClass, List<GisFeature> gisFeatures, List<FieldDescription> fields)
  {
    foreach (GisFeature feat in gisFeatures)
    {
      using (RowBuffer rowBuffer = newFeatureClass.CreateRowBuffer())
      {
        newFeatureClass.CreateRow(AssignFieldValuesToRow(rowBuffer, fields, feat)).Dispose();
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
        newFeatureClass.CreateRow(AssignFieldValuesToRow(rowBuffer, fields, feat)).Dispose();
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

  public string CleanCharacters(string key)
  {
    Regex rg = new(@"^[a-zA-Z0-9_]*$");
    if (rg.IsMatch(key))
    {
      return key;
    }

    string result = "";
    foreach (char c in key)
    {
      Regex rg_character = new(@"^[a-zA-Z0-9_]*$");
      if (rg_character.IsMatch(c.ToString()))
      {
        result += c.ToString();
      }
      else
      {
        result += "_";
      }
    }
    return key.Replace(" ", "_").Replace("%", "_").Replace("$", "_");
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
            string key = field.Key;
            FieldType fieldType = GetFieldTypeFromInt((int)(long)field.Value);

            FieldDescription fiendDescription = new(CleanCharacters(key), fieldType) { AliasName = key };
            fields.Add(fiendDescription);
            fieldAdded.Add(key);
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
    string? originalGeomType = target.geomType != null ? target.geomType : target.nativeGeomType;
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
