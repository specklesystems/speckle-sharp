using Autodesk.Revit.DB;
using System;

namespace Objects.Converter.Revit;

public static class RevitVersionHelper
{
  public static string Version
  {
    get
    {
#if REVIT2023
      return "2023";
#elif REVIT2022
      return "2022";
#elif REVIT2021
      return "2021";
#elif REVIT2020
      return "2020";
#else
      return "2024";
#endif
    }
  }

  /// <summary>
  /// Converts to internal units by using the speckle parameter applicationUnit
  /// </summary>
  /// <param name="parameter">Speckle parameter</param>
  /// <returns></returns>
  public static double ConvertToInternalUnits(object value, string applicationUnit)
  {
#if REVIT2020
    _ = Enum.TryParse(applicationUnit, out DisplayUnitType sourceUnit);
    return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(value), sourceUnit);
#else
    // if a commit is sent in <=2021 and received in 2022+, the application unit will be a different format
    // therefore we need to check if the applicationUnit is in the wrong format
    ForgeTypeId sourceUnit = null;
    if (
      !string.IsNullOrEmpty(applicationUnit) && applicationUnit.Length >= 3 && applicationUnit.Substring(0, 3) == "DUT"
    )
    {
      sourceUnit = DUTToForgeTypeId(applicationUnit);
    }
    else
    {
      sourceUnit = new ForgeTypeId(applicationUnit);
    }

    return UnitUtils.ConvertToInternalUnits(Convert.ToDouble(value), sourceUnit);
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
#if REVIT2020
    return UnitUtils.ConvertToInternalUnits(value, parameter.DisplayUnitType);
#else
    return UnitUtils.ConvertToInternalUnits(value, parameter.GetUnitTypeId());
#endif
  }

  public static string GetUnityTypeString(this Definition definition)
  {
#if REVIT2020 || REVIT2021
    return definition.UnitType.ToString();
#else
    return definition.GetDataType().TypeId;
#endif
  }

#if REVIT2020
  public static DisplayUnitType GetUnitTypeId(this Parameter parameter)
  {
    return parameter.DisplayUnitType;
  }
#else
  public static ForgeTypeId GetUnitTypeId(this Parameter parameter)
  {
    return parameter.GetUnitTypeId();
  }
#endif

  public static bool IsCurveClosed(NurbSpline curve)
  {
#if REVIT2020
    return curve.isClosed;
#else
    try
    {
      return curve.IsClosed;
    }
    catch (Autodesk.Revit.Exceptions.ApplicationException)
    {
      return true;
    }
#endif
  }

  public static bool IsCurveClosed(Curve curve)
  {
#if REVIT2020
    if (curve.IsBound && curve.GetEndPoint(0).IsAlmostEqualTo(curve.GetEndPoint(1)))
    {
      return true;
    }
    else if (!curve.IsBound && curve.IsCyclic)
    {
      return true;
    }

    return false;
#else

    try
    {
      return curve.IsClosed;
    }
    catch (Autodesk.Revit.Exceptions.ApplicationException)
    {
      return true;
    }
#endif
  }

#if REVIT2020
#else
  private static ForgeTypeId DUTToForgeTypeId(string s)
  {
    ForgeTypeId sourceUnit = null;
    switch (s.ToLower())
    {
      case string a when a.Contains("millimeters"):
        sourceUnit = UnitTypeId.Millimeters;
        break;
      case string a when a.Contains("centimeters"):
        sourceUnit = UnitTypeId.Centimeters;
        break;
      case string a when a.Contains("meters"):
        sourceUnit = UnitTypeId.Centimeters;
        break;
      case string a when a.Contains("centimeters"):
        sourceUnit = UnitTypeId.Meters;
        break;
      case string a when a.Contains("feet") && a.Contains("inches"):
        sourceUnit = UnitTypeId.FeetFractionalInches;
        break;
      case string a when a.Contains("feet"):
        sourceUnit = UnitTypeId.Feet;
        break;
      case string a when a.Contains("inches"):
        sourceUnit = UnitTypeId.Inches;
        break;
      case string a when a.Contains("degrees"):
        sourceUnit = UnitTypeId.Degrees;
        break;
      case string a when a.Contains("radians"):
        sourceUnit = UnitTypeId.Radians;
        break;
    }

    return sourceUnit;
  }
#endif
}
