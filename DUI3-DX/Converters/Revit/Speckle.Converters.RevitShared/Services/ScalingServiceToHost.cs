using Autodesk.Revit.DB;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.Services;

public sealed class ScalingServiceToHost
{
  public double ScaleToNative(double value, string units)
  {
    if (string.IsNullOrEmpty(units))
    {
      return value;
    }

    return UnitUtils.ConvertToInternalUnits(value, UnitsToNative(units));
  }

  public ForgeTypeId UnitsToNative(string units)
  {
    var u = Core.Kits.Units.GetUnitsFromString(units);
    switch (u)
    {
      case Core.Kits.Units.Millimeters:
        return UnitTypeId.Millimeters;
      case Core.Kits.Units.Centimeters:
        return UnitTypeId.Centimeters;
      case Core.Kits.Units.Meters:
        return UnitTypeId.Meters;
      case Core.Kits.Units.Inches:
        return UnitTypeId.Inches;
      case Core.Kits.Units.Feet:
        return UnitTypeId.Feet;
      default:
        throw new SpeckleConversionException($"The Unit System \"{units}\" is unsupported.");
    }
  }
}
