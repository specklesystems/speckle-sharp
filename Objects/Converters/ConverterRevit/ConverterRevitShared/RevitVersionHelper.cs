using Autodesk.Revit.DB;
using System;

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

    /// <summary>
    /// Converts to internal units by using the speckle parameter applicationUnit
    /// </summary>
    /// <param name="parameter">Speckle parameter</param>
    /// <returns></returns>
    public static double ConvertToInternalUnits(Objects.BuiltElements.Revit.Parameter parameter)
    {
#if !(REVIT2022 || REVIT2023)
      Enum.TryParse(parameter.applicationUnit, out DisplayUnitType sourceUnit);
      return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(parameter.value), sourceUnit);
#else
      var sourceUnit = new ForgeTypeId(parameter.applicationUnit);
      return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(parameter.value), sourceUnit);
#endif
    }

    /// <summary>
    /// Converts to internal units by using the destination parameter Display Units
    /// </summary>
    /// <param name="value">Value to set</param>
    /// <param name="parameter">Destination parameter</param>
    /// <returns></returns>
    public static double ConvertToInternalUnits(double value, Parameter parameter)
    {
#if !(REVIT2022 || REVIT2023)
      return UnitUtils.ConvertToInternalUnits(value, parameter.DisplayUnitType);
#else
      return UnitUtils.ConvertToInternalUnits(value, parameter.GetUnitTypeId());
#endif
    }

    public static double ConvertFromInternalUnits(double val, Parameter parameter)
    {
#if !(REVIT2022 || REVIT2023)
      return UnitUtils.ConvertFromInternalUnits(val, parameter.DisplayUnitType);
#else
      return UnitUtils.ConvertFromInternalUnits(val, parameter.GetUnitTypeId());
#endif
    }

    public static string GetUnityTypeString(this Parameter parameter)
    {
#if !(REVIT2022 || REVIT2023)
      return parameter.Definition.UnitType.ToString();
#else
      return parameter.Definition.GetDataType().TypeId;
#endif
    }

    public static string GetDisplayUnityTypeString(this Parameter parameter)
    {
#if !(REVIT2022 || REVIT2023)
      return parameter.DisplayUnitType.ToString();
#else
      return parameter.GetUnitTypeId().TypeId;
#endif
    }



    public static bool IsCurveClosed(NurbSpline curve)
    {
#if (REVIT2021 || REVIT2022 || REVIT2023)
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
#if (REVIT2021 || REVIT2022 || REVIT2023)
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

