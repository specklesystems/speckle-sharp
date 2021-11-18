using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.Revit
{
  public static class RevitVersionHelper
  {

    public static string Version
    {
      get
      {
#if REVIT2022
          return "2022";
#elif REVIT2021
        return "2021";
#elif REVIT2020
        return "2020";
#else
        return "2019";
#endif
      }
    }

    public static double ConvertToInternalUnits(Objects.BuiltElements.Revit.Parameter parameter)
    {
#if !(REVIT2022)
      Enum.TryParse(parameter.applicationUnit, out DisplayUnitType sourceUnit);
      return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(parameter.value), sourceUnit);
#else
      var sourceUnit = new ForgeTypeId(parameter.applicationUnit);
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
#if !(REVIT2022)
      return parameter.DisplayUnitType.ToString();
#else
      return parameter.GetUnitTypeId().TypeId;
#endif
    }



    public static bool IsCurveClosed(NurbSpline curve)
    {
#if (REVIT2021 || REVIT2022)
      try
      {
        return curve.IsClosed;
      }
      catch
      {
        return true;
      }
#else
      return curve.isClosed;
#endif
    }

    public static bool IsCurveClosed(Curve curve)
    {
#if (REVIT2021 || REVIT2022)
      try
      {
        return curve.IsClosed;
      }
      catch
      {
        return true;
      }
#else
      if (curve.IsBound && curve.GetEndPoint(0).IsAlmostEqualTo(curve.GetEndPoint(1)))
        return true;
      else if (!curve.IsBound && curve.IsCyclic)
        return true;
      return false;
#endif
    }
  }
}
