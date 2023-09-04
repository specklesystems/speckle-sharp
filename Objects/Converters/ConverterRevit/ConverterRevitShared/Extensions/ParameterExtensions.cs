#nullable enable
using System;
using Autodesk.Revit.DB;
using Objects.Converter.Revit;

namespace ConverterRevitShared.Extensions
{
  internal static class ParameterExtensions
  {
    public static object? GetValue(
      this Parameter parameter,
      Definition definition,
#if REVIT2020
      DisplayUnitType? unitTypeId = null
#else
      ForgeTypeId? unitTypeId = null
#endif
    )
    {
      switch (parameter.StorageType)
      {
        case StorageType.Double:
          var val = parameter.AsDouble();
          if (val == default(double) && parameter.HasValue == false)
          {
            return null;
          }
          return ConverterRevit.ScaleToSpeckleStatic(val, unitTypeId ?? parameter.GetUnitTypeId());
        case StorageType.Integer:
          var intVal = parameter.AsInteger();
          if (intVal == default(int) && parameter.HasValue == false)
          {
            return null;
          }
          return definition.IsBool() ? Convert.ToBoolean(intVal) : intVal;

        case StorageType.String:
          return parameter.AsString();
        case StorageType.ElementId:
          return parameter.AsElementId().ToString();
        default:
          return null;
      }
    }
  }
}
