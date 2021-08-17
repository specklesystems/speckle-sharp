using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Speckle.Core.Serialisation
{
  internal static class ValueConverter
  {

    public static bool ConvertValue(Type type, object value, out object convertedValue)
    {
      convertedValue = null;
      Type valueType = value.GetType();
      if (type.IsAssignableFrom(valueType))
      {
        convertedValue = value;
        return true;
      }

      bool isList = value is List<object>;
      List<object> valueList = value as List<object>;

      #region Enum
      if (type.IsEnum)
      {
        if (valueType != typeof(double)) return false;
        convertedValue = Enum.ToObject(type, (long)(double)value);
        return true;
      }
      #endregion

      switch (type.Name)
      {
        #region Numbers
        case "Int64":
          if (valueType == typeof(double)) { convertedValue = (Int64)(double)value; return true; }
          else return false;
        case "Int32":
          if (valueType == typeof(double)) { convertedValue = (Int32)(double)value; return true; }
          else return false;
        case "Int16":
          if (valueType == typeof(double)) { convertedValue = (Int16)(double)value; return true; }
          else return false;
        case "UInt64":
          if (valueType == typeof(double)) { convertedValue = (UInt64)(double)value; return true; }
          else return false;
        case "UInt32":
          if (valueType == typeof(double)) { convertedValue = (UInt32)(double)value; return true; }
          else return false;
        case "UInt16":
          if (valueType == typeof(double)) { convertedValue = (UInt16)(double)value; return true; }
          else return false;
        #endregion

        #region Arrays
        case "Int64[]":
          if (!isList) return false;
          Int64[] ret_int64arr = new Int64[valueList.Count];
          for (int i = 0; i < valueList.Count; i++)
          {
            if (valueList[i].GetType() != typeof(double)) return false;
            ret_int64arr[i] = (Int64)(double)valueList[i];
          }
          convertedValue = ret_int64arr;
          return true;
        case "Int32[]":
          if (!isList) return false;
          Int32[] ret_int32arr = new Int32[valueList.Count];
          for (int i = 0; i < valueList.Count; i++)
          {
            if (valueList[i].GetType() != typeof(double)) return false;
            ret_int32arr[i] = (Int32)(double)valueList[i];
          }
          convertedValue = ret_int32arr;
          return true;
        case "Int16[]":
          if (!isList) return false;
          Int16[] ret_int16arr = new Int16[valueList.Count];
          for (int i = 0; i < valueList.Count; i++)
          {
            if (valueList[i].GetType() != typeof(double)) return false;
            ret_int16arr[i] = (Int16)(double)valueList[i];
          }
          convertedValue = ret_int16arr;
          return true;
        case "UInt64[]":
          if (!isList) return false;
          UInt64[] ret_uint64arr = new UInt64[valueList.Count];
          for (int i = 0; i < valueList.Count; i++)
          {
            if (valueList[i].GetType() != typeof(double)) return false;
            ret_uint64arr[i] = (UInt64)(double)valueList[i];
          }
          convertedValue = ret_uint64arr;
          return true;
        case "UInt32[]":
          if (!isList) return false;
          UInt32[] ret_uint32arr = new UInt32[valueList.Count];
          for (int i = 0; i < valueList.Count; i++)
          {
            if (valueList[i].GetType() != typeof(double)) return false;
            ret_uint32arr[i] = (UInt32)(double)valueList[i];
          }
          convertedValue = ret_uint32arr;
          return true;
        case "UInt16[]":
          if (!isList) return false;
          UInt16[] ret_uint16arr = new UInt16[valueList.Count];
          for (int i = 0; i < valueList.Count; i++)
          {
            if (valueList[i].GetType() != typeof(double)) return false;
            ret_uint16arr[i] = (UInt16)(double)valueList[i];
          }
          convertedValue = ret_uint16arr;
          return true;

        #endregion

        #region List
        case "List`1":
          if (!isList) return false;
          Type listElementType = type.GenericTypeArguments[0];
          object ret = Activator.CreateInstance(type, new object[] { valueList.Count });
          var addMethod = type.GetMethod("Add");
          foreach (object inputListElement in valueList)
          {
            object convertedListElement;
            if (!ConvertValue(listElementType, inputListElement, out convertedListElement))
              return false;
            addMethod.Invoke(ret, new object[] { convertedListElement });
          }
          convertedValue = ret;
          return true;
        #endregion
      }

      return false;
    }
  }
}
