using Rhino;
using Speckle.Converters.Common;
using Speckle.Core.Kits;

namespace Speckle.Converters.Rhino7;

public class RhinoToSpeckleUnitConverter : IHostToSpeckleUnitConverter<UnitSystem>
{
  private readonly Dictionary<UnitSystem, string> _unitMapping = new();

  public RhinoToSpeckleUnitConverter()
  {
    // POC: CNX-9269 Add unit test to ensure these don't change.
    _unitMapping[UnitSystem.None] = Units.Meters;
    _unitMapping[UnitSystem.Millimeters] = Units.Millimeters;
    _unitMapping[UnitSystem.Centimeters] = Units.Centimeters;
    _unitMapping[UnitSystem.Meters] = Units.Meters;
    _unitMapping[UnitSystem.Kilometers] = Units.Kilometers;
    _unitMapping[UnitSystem.Inches] = Units.Inches;
    _unitMapping[UnitSystem.Feet] = Units.Feet;
    _unitMapping[UnitSystem.Yards] = Units.Yards;
    _unitMapping[UnitSystem.Miles] = Units.Miles;
    _unitMapping[UnitSystem.Unset] = Units.Meters;
  }

  public string ConvertOrThrow(UnitSystem hostUnit)
  {
    if (_unitMapping.TryGetValue(hostUnit, out string value))
    {
      return value;
    }

    throw new SpeckleConversionException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
