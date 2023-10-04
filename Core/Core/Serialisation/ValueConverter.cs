#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Numerics = System.Numerics;
using System.DoubleNumerics;
using Speckle.Core.Logging;

namespace Speckle.Core.Serialisation;

internal static class ValueConverter
{
  public static bool ConvertValue(Type type, object? value, out object? convertedValue)
  {
    // TODO: Document list of supported values in the SDK. (and grow it as needed)

    convertedValue = null;
    if (value == null)
      return true;
    Type valueType = value.GetType();

    if (type.IsAssignableFrom(valueType))
    {
      convertedValue = value;
      return true;
    }

    bool isList = value is List<object>;
    List<object> valueList = value as List<object>;

    //strings
    if (type == typeof(string))
    {
      convertedValue = Convert.ToString(value);
      return true;
    }

    #region Enum
    if (type.IsEnum)
    {
      if (valueType != typeof(long))
        return false;
      convertedValue = Enum.ToObject(type, (long)value);
      return true;
    }
    #endregion

    switch (type.Name)
    {
      case "Nullable`1":
        return ConvertValue(type.GenericTypeArguments[0], value, out convertedValue);
      #region Numbers
      case "Int64":
        if (valueType == typeof(long))
        {
          convertedValue = (long)value;
          return true;
        }

        return false;
      case "Int32":
        if (valueType == typeof(long))
        {
          convertedValue = (int)(long)value;
          return true;
        }

        return false;
      case "Int16":
        if (valueType == typeof(long))
        {
          convertedValue = (short)(long)value;
          return true;
        }

        return false;
      case "UInt64":
        if (valueType == typeof(long))
        {
          convertedValue = (ulong)(long)value;
          return true;
        }

        return false;
      case "UInt32":
        if (valueType == typeof(long))
        {
          convertedValue = (uint)(long)value;
          return true;
        }

        return false;
      case "UInt16":
        if (valueType == typeof(long))
        {
          convertedValue = (ushort)(long)value;
          return true;
        }

        return false;
      case "Double":
        if (valueType == typeof(double))
        {
          convertedValue = (double)value;
          return true;
        }
        if (valueType == typeof(long))
        {
          convertedValue = (double)(long)value;
          return true;
        }
        switch (value)
        {
          case "NaN":
            convertedValue = double.NaN;
            return true;
          case "Infinity":
            convertedValue = double.PositiveInfinity;
            return true;
          case "-Infinity":
            convertedValue = double.NegativeInfinity;
            return true;
          default:
            return false;
        }

      case "Single":
        if (valueType == typeof(double))
        {
          convertedValue = (float)(double)value;
          return true;
        }
        if (valueType == typeof(long))
        {
          convertedValue = (float)(long)value;
          return true;
        }
        switch (value)
        {
          case "NaN":
            convertedValue = float.NaN;
            return true;
          case "Infinity":
            convertedValue = float.PositiveInfinity;
            return true;
          case "-Infinity":
            convertedValue = float.NegativeInfinity;
            return true;
          default:
            return false;
        }

      #endregion
    }

    // Handle List<?>
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
    {
      if (!isList)
        return false;
      Type listElementType = type.GenericTypeArguments[0];
      IList ret = Activator.CreateInstance(type, valueList.Count) as IList;
      foreach (object inputListElement in valueList)
      {
        object convertedListElement;
        if (!ConvertValue(listElementType, inputListElement, out convertedListElement))
          return false;
        ret.Add(convertedListElement);
      }
      convertedValue = ret;
      return true;
    }

    // Handle Dictionary<string,?>
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    {
      if (value is not Dictionary<string, object> valueDict)
        return false;

      if (type.GenericTypeArguments[0] != typeof(string))
        throw new ArgumentException("Dictionaries with non-string keys are not supported", nameof(type));
      Type dictValueType = type.GenericTypeArguments[1];
      IDictionary ret = Activator.CreateInstance(type) as IDictionary;

      foreach (KeyValuePair<string, object> kv in valueDict)
      {
        if (!ConvertValue(dictValueType, kv.Value, out object convertedDictValue))
          return false;
        ret[kv.Key] = convertedDictValue;
      }
      convertedValue = ret;
      return true;
    }

    // Handle arrays
    if (type.IsArray)
    {
      if (!isList)
        return false;
      Type arrayElementType =
        type.GetElementType() ?? throw new ArgumentException("IsArray yet not valid element type", nameof(type));

      Array ret = Activator.CreateInstance(type, valueList.Count) as Array;
      for (int i = 0; i < valueList.Count; i++)
      {
        object inputListElement = valueList[i];
        if (!ConvertValue(arrayElementType, inputListElement, out object convertedListElement))
          return false;
        ret.SetValue(convertedListElement, i);
      }
      convertedValue = ret;
      return true;
    }

    // Handle simple classes/structs
    if (type == typeof(Guid) && value is string str)
    {
      convertedValue = Guid.Parse(str);
      return true;
    }

    if (type == typeof(Color) && value is long integer)
    {
      convertedValue = Color.FromArgb((int)integer);
      return true;
    }

    if (type == typeof(DateTime) && value is string s)
    {
      convertedValue = DateTime.ParseExact(s, "o", CultureInfo.InvariantCulture);
      return true;
    }

    #region BACKWARDS COMPATIBILITY: matrix4x4 changed from System.Numerics float to System.DoubleNumerics double in release 2.16
    if (type == typeof(Numerics.Matrix4x4) && value is IReadOnlyList<object> lMatrix)
    {
      SpeckleLog.Logger.Warning("This kept for backwards compatibility, no one should be using {this}", "ValueConverter deserialize to System.Numerics.Matrix4x4");
      float I(int index) => Convert.ToSingle(lMatrix[index]);
      convertedValue = new Numerics.Matrix4x4(
        I(0),
        I(1),
        I(2),
        I(3),
        I(4),
        I(5),
        I(6),
        I(7),
        I(8),
        I(9),
        I(10),
        I(11),
        I(12),
        I(13),
        I(14),
        I(15)
      );
      return true;
    }
    #endregion

    if (type == typeof(Matrix4x4) && value is IReadOnlyList<object> l)
    {
      double I(int index) => Convert.ToDouble(l[index]);
      convertedValue = new Matrix4x4(
        I(0),
        I(1),
        I(2),
        I(3),
        I(4),
        I(5),
        I(6),
        I(7),
        I(8),
        I(9),
        I(10),
        I(11),
        I(12),
        I(13),
        I(14),
        I(15)
      );
      return true;
    }

    return false;
  }
}
