using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Objects.Primitive;

namespace ConverterRevitShared.Extensions
{
  public static class DefinitionExtensions
  {
    public static bool IsBool(this Definition definition)
    {
#if REVIT2020 || REVIT2021 || REVIT2022
      switch (definition.ParameterType)
      {
        case ParameterType.YesNo:
          return true;
        default:
          return false;
      }
#else
      if (definition.GetDataType() == SpecTypeId.Boolean.YesNo)
        return true;
      else
        return false;
#endif
    }
  }
}
