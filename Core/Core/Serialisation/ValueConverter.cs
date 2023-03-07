using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Speckle.Core.Serialisation
{
  internal static class ValueConverter
  {

    public static bool ConvertValue(Type type, object value, out object convertedValue)
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
        if (valueType != typeof(long)) return false;
        convertedValue = Enum.ToObject(type, (long)value);
        return true;
      }
      #endregion

      switch (type.Name)
      {
        case "Nullable`1":
          if (value == null) { convertedValue = null; return true; }
          return ConvertValue(type.GenericTypeArguments[0], value, out convertedValue);
        #region Numbers
        case "Int64":
          if (valueType == typeof(long)) { convertedValue = (long)value; return true; }
          else return false;
        case "Int32":
          if (valueType == typeof(long)) { convertedValue = (Int32)(long)value; return true; }
          else return false;
        case "Int16":
          if (valueType == typeof(long)) { convertedValue = (Int16)(long)value; return true; }
          else return false;
        case "UInt64":
          if (valueType == typeof(long)) { convertedValue = (UInt64)(long)value; return true; }
          else return false;
        case "UInt32":
          if (valueType == typeof(long)) { convertedValue = (UInt32)(long)value; return true; }
          else return false;
        case "UInt16":
          if (valueType == typeof(long)) { convertedValue = (UInt16)(long)value; return true; }
          else return false;
        case "Double":
          if (valueType == typeof(double)) { convertedValue = (Double)(double)value; return true; }
          if (valueType == typeof(long)) { convertedValue = (Double)(long)value; return true; }
          else return false;
        case "Single":
          if (valueType == typeof(double)) { convertedValue = (Single)(double)value; return true; }
          if (valueType == typeof(long)) { convertedValue = (Single)(long)value; return true; }
          else return false;
          #endregion
      }

      // Handle List<?>
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
      {
        if (!isList) return false;
        Type listElementType = type.GenericTypeArguments[0];
        IList ret = Activator.CreateInstance(type, new object[] { valueList.Count }) as IList;
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
        if (!(value is Dictionary<string, object>))
          return false;
        Dictionary<string, object> valueDict = (Dictionary<string, object>)value;

        if (type.GenericTypeArguments[0] != typeof(string))
          throw new Exception("Dictionaries with non-string keys are not supported");
        Type dictValueType = type.GenericTypeArguments[1];
        IDictionary ret = Activator.CreateInstance(type) as IDictionary;

        foreach (KeyValuePair<string, object> kv in valueDict)
        {
          object convertedDictValue;
          if (!ConvertValue(dictValueType, kv.Value, out convertedDictValue))
            return false;
          ret[kv.Key] = convertedDictValue;
        }
        convertedValue = ret;
        return true;
      }

      // Handle arrays
      if (type.IsArray)
      {
        if (!isList) return false;
        Type arrayElementType = type.GetElementType();
        Array ret = Activator.CreateInstance(type, new object[] { valueList.Count }) as Array;
        for (int i = 0; i < valueList.Count; i++)
        {
          object inputListElement = valueList[i];
          object convertedListElement;
          if (!ConvertValue(arrayElementType, inputListElement, out convertedListElement))
            return false;
          ret.SetValue(convertedListElement, i);
        }
        convertedValue = ret;
        return true;
      }

      // Handle simple classes/structs
      if (type == typeof(Guid) && valueType == typeof(string))
      {
        convertedValue = Guid.Parse(value as string);
        return true;
      }

      if (type == typeof(System.Drawing.Color) && valueType == typeof(long))
      {
        convertedValue = System.Drawing.Color.FromArgb((int)(long)value);
        return true;
      }

      if (type == typeof(DateTime) && valueType == typeof(string))
      {
        convertedValue = DateTime.ParseExact((string)value, "o", System.Globalization.CultureInfo.InvariantCulture);
        return true;
      }

      if (type == typeof(Matrix4x4) && valueType == typeof(List<object>))
      {
        var l = (value as List<object>).ToList();
        float I(int index) => Convert.ToSingle(l[index]);
        convertedValue = new Matrix4x4(
          I(0), I(1), I(2), I(3),
          I(4), I(5), I(6), I(7),
          I(8), I(9), I(10), I(11),
          I(12), I(13), I(14), I(15)
          );
        return true;
      }

      return false;
    }
  }
}
