using Rhino;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging; // POC: boy do I think this is the wrong place for SpeckleException!

namespace Speckle.Converters.Rhino7;

public class RhinoToSpeckleUnitConverter : IHostToSpeckleUnitConverter<UnitSystem>
{
  private readonly Dictionary<UnitSystem, string> _unitMapping = new();

  public RhinoToSpeckleUnitConverter()
  {
    // POC: we should have a unit test to confirm these are as expected and don't change
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

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
