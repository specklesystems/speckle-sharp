using ArcGIS.Core.Data;

namespace Speckle.Converters.ArcGIS3.Utils;

public static class GISAttributeFieldType
{
  public const string GUID_TYPE = "Guid";
  public const string OID = "Oid"; // object identifier: int
  public const string STRING_TYPE = "String";
  public const string FLOAT_TYPE = "Float"; // single-precision floating point number
  public const string INTEGER_TYPE = "Integer"; // 32-bit int
  public const string BIGINTEGER = "BigInteger"; // 64-bit int
  public const string SMALLINTEGER = "SmallInteger"; // 16-bit int
  public const string DOUBLE_TYPE = "Double";
  public const string DATETIME = "DateTime";
  public const string DATEONLY = "DateOnly";
  public const string TIMEONLY = "TimeOnly";
  public const string TIMESTAMPOFFSET = "TimeStampOffset";

  public static string FieldTypeToSpeckle(FieldType fieldType)
  {
    return fieldType switch
    {
      FieldType.GUID => GUID_TYPE,
      FieldType.OID => OID,
      FieldType.String => STRING_TYPE,
      FieldType.Single => FLOAT_TYPE,
      FieldType.Integer => INTEGER_TYPE,
      FieldType.BigInteger => BIGINTEGER,
      FieldType.SmallInteger => SMALLINTEGER,
      FieldType.Double => DOUBLE_TYPE,
      FieldType.Date => DATETIME,
      FieldType.DateOnly => DATEONLY,
      FieldType.TimeOnly => TIMEONLY,
      FieldType.TimestampOffset => TIMESTAMPOFFSET,
      _ => throw new ArgumentOutOfRangeException(nameof(fieldType)),
    };
  }

  public static FieldType FieldTypeToNative(object fieldType)
  {
    if (fieldType is string fieldStringType)
    {
      return fieldStringType switch
      {
        GUID_TYPE => FieldType.GUID,
        OID => FieldType.OID,
        STRING_TYPE => FieldType.String,
        FLOAT_TYPE => FieldType.Single,
        INTEGER_TYPE => FieldType.Integer,
        BIGINTEGER => FieldType.BigInteger,
        SMALLINTEGER => FieldType.SmallInteger,
        DOUBLE_TYPE => FieldType.Double,
        DATETIME => FieldType.Date,
        DATEONLY => FieldType.DateOnly,
        TIMEONLY => FieldType.TimeOnly,
        TIMESTAMPOFFSET => FieldType.String, // sending and receiving as stings
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType)),
      };
    }
    // old way:
    return (FieldType)(int)(long)fieldType;
  }

  public static object? FieldValueToSpeckle(Row row, Field field)
  {
    if (
      field.FieldType == FieldType.DateOnly
      || field.FieldType == FieldType.TimeOnly
      || field.FieldType == FieldType.TimestampOffset
    )
    {
      return row[field.Name]?.ToString();
    }
    else
    {
      return row[field.Name];
    }
  }

  public static object? SpeckleValueToNativeFieldType(FieldType fieldType, object? value)
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
        return fieldType switch
        {
          FieldType.String => (string)value,
          FieldType.Single => Convert.ToSingle(value),
          FieldType.Integer => Convert.ToInt32(value), // need this step because sent "ints" seem to be received as "longs"
          FieldType.BigInteger => Convert.ToInt64(value),
          FieldType.SmallInteger => Convert.ToInt16(value),
          FieldType.Double => Convert.ToDouble(value),
          FieldType.Date => DateTime.Parse((string)value, null),
          FieldType.DateOnly => DateOnly.Parse((string)value),
          FieldType.TimeOnly => TimeOnly.Parse((string)value),
          _ => value,
        };
      }
      catch (InvalidCastException)
      {
        return value;
      }
    }

    return value;
  }

  public static FieldType GetFieldTypeFromRawValue(object? value)
  {
    // using "Blob" as a placeholder for unrecognized values/nulls.
    // Once all elements are iterated, FieldType.Blob will be replaced with FieldType.String if no better type found
    if (value is not null)
    {
      return value switch
      {
        string => FieldType.String,
        int => FieldType.Integer,
        long => FieldType.BigInteger,
        double => FieldType.Double,
        _ => FieldType.Blob,
      };
    }

    return FieldType.Blob;
  }
}
