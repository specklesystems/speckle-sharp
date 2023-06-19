using Objects.Structural.Analysis;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

internal sealed class RhinoUnits : IRhinoUnits
{
  public string UnitToSpeckle(UnitSystem us)
  {
    switch (us)
    {
      case UnitSystem.None:
        return Units.Meters;

      case UnitSystem.Millimeters:
        return Units.Millimeters;

      case UnitSystem.Centimeters:
        return Units.Centimeters;

      case UnitSystem.Meters:
        return Units.Meters;

      case UnitSystem.Kilometers:
        return Units.Kilometers;

      case UnitSystem.Inches:
        return Units.Inches;

      case UnitSystem.Feet:
        return Units.Feet;

      case UnitSystem.Yards:
        return Units.Yards;

      case UnitSystem.Miles:
        return Units.Miles;

      case UnitSystem.Unset:
        return Units.Meters;

      default:
        throw new SpeckleException($"The Unit System \"{us}\" is unsupported.");
    }
  }

  public double ScaleToNative(double value, string units, string modelUnits)
  {
    return value * Units.GetConversionFactor(units, modelUnits);
  }
}
