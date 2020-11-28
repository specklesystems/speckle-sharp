using Autodesk.Revit.DB;
using System;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private object ParameterToSpeckle(Parameter param)
    {
      switch (param.StorageType)
      {
        case StorageType.Double:
          var doubleValue = param.AsDouble();
          var convertedDoubleValue = UnitUtils.ConvertFromInternalUnits(doubleValue, param.DisplayUnitType);
          return convertedDoubleValue;

        case StorageType.ElementId:
          var elementId = param.AsElementId();
          var element = Doc.GetElement(elementId);
          if (element == null)
          {
            return null;
          }

          return ConvertToSpeckle(element);

        case StorageType.Integer:
          switch (param.Definition.ParameterType)
          {
            case ParameterType.YesNo:
              return Convert.ToBoolean(param.AsInteger());

            default:
              return param.AsInteger();
          }
        case StorageType.String:
          var asString = param.AsString();
          if (asString == null)
          {
            return param.AsValueString();
          }

          return asString;

        default:
          throw new Exception("Cannot convert param of type " + param.StorageType);
      }
    }
  }
}