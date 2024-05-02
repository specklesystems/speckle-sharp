using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using Objects.GIS;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public class FieldsUtils : IFieldsUtils
{
  private readonly IOtherUtils _otherUtils;
  private const string FID_FIELD_NAME = "OBJECTID";

  public FieldsUtils(IOtherUtils otherUtils)
  {
    _otherUtils = otherUtils;
  }

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

      if (value is string stringValue)
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
            string key = field.Key;
            FieldType fieldType = GetFieldTypeFromInt((int)(long)field.Value);

            FieldDescription fieldDescription = new(_otherUtils.CleanCharacters(key), fieldType) { AliasName = key };
            fields.Add(fieldDescription);
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
}
