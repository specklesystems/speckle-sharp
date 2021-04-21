using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.Revit
{
  public static class RevitVersionHelper
  {

    public static double ConvertToInternalUnits(Objects.BuiltElements.Revit.Parameter parameter)
    {
#if !(REVIT2022)
      Enum.TryParse(parameter.revitUnit, out DisplayUnitType sourceUnit);
      return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(parameter.value), sourceUnit);
#else
      var sourceUnit = new ForgeTypeId(parameter.revitUnit);
      return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(parameter.value), sourceUnit);
#endif
    }

    public static double ConvertFromInternalUnits(double val, Parameter parameter)
    {
#if !(REVIT2022)
      return UnitUtils.ConvertFromInternalUnits(val, parameter.DisplayUnitType);
#else
      return UnitUtils.ConvertFromInternalUnits(val, parameter.GetUnitTypeId());
#endif
    }

    public static string GetUnityTypeString(this Parameter parameter)
    {
#if !(REVIT2022)
      return parameter.Definition.UnitType.ToString();
#else
      return parameter.Definition.GetDataType().TypeId;
#endif
    }

    public static string GetDisplayUnityTypeString(this Parameter parameter)
    {
#if (REVIT2019 || REVIT2020 || REVIT2021)
      return parameter.DisplayUnitType.ToString();
#else
      return parameter.GetUnitTypeId().TypeId;
#endif
    }



    public static bool IsCurveClosed(NurbSpline curve)
    {
#if (REVIT2019 || REVIT2020)
      return curve.isClosed;
#else
      // dynamo for revit also uses this converter
      // but it default to the 2021 version, so if this method is called 
      // by an earlier version it might throw
      try
      {
        return curve.IsClosed;
      }
      catch
      {
        return true;
      }
#endif
    }

    public static bool IsCurveClosed(Curve curve)
    {
#if (REVIT2019 || REVIT2020)
      if (curve.IsBound && curve.GetEndPoint(0).IsAlmostEqualTo(curve.GetEndPoint(1)))
        return true;
      else if (!curve.IsBound && curve.IsCyclic)
        return true;
      return false;
#else
      // dynamo for revit also uses this converter
      // but it default to the 2021 version, so if this method is called 
      // by an earlier version it might throw
      try
      {
        return curve.IsClosed;
      }
      catch
      {
        return true;
      }
#endif
    }
  }
}
