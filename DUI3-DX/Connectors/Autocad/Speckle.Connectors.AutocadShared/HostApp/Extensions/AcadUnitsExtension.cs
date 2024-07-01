using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Autocad.HostApp.Extensions;

public static class AcadUnitsExtension
{
  public static string ToSpeckleString(this UnitsValue units)
  {
    switch (units)
    {
      case UnitsValue.Millimeters:
        return Units.Millimeters;
      case UnitsValue.Centimeters:
        return Units.Centimeters;
      case UnitsValue.Meters:
        return Units.Meters;
      case UnitsValue.Kilometers:
        return Units.Kilometers;
      case UnitsValue.Inches:
      case UnitsValue.USSurveyInch:
        return Units.Inches;
      case UnitsValue.Feet:
      case UnitsValue.USSurveyFeet:
        return Units.Feet;
      case UnitsValue.Yards:
      case UnitsValue.USSurveyYard:
        return Units.Yards;
      case UnitsValue.Miles:
      case UnitsValue.USSurveyMile:
        return Units.Miles;
      case UnitsValue.Undefined:
        return Units.None;
      default:
        throw new SpeckleException($"The Unit System \"{units}\" is unsupported.");
    }
  }
}
