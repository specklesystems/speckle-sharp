using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7;

public class RhinoToSpeckleUnitConverter : IHostToSpeckleUnitConverter<RhinoUnitSystem>
{
  private readonly Dictionary<RhinoUnitSystem, string> _unitMapping = new();

  public RhinoToSpeckleUnitConverter()
  {
    // POC: CNX-9269 Add unit test to ensure these don't change.
    _unitMapping[RhinoUnitSystem.None] = Units.Meters;
    _unitMapping[RhinoUnitSystem.Millimeters] = Units.Millimeters;
    _unitMapping[RhinoUnitSystem.Centimeters] = Units.Centimeters;
    _unitMapping[RhinoUnitSystem.Meters] = Units.Meters;
    _unitMapping[RhinoUnitSystem.Kilometers] = Units.Kilometers;
    _unitMapping[RhinoUnitSystem.Inches] = Units.Inches;
    _unitMapping[RhinoUnitSystem.Feet] = Units.Feet;
    _unitMapping[RhinoUnitSystem.Yards] = Units.Yards;
    _unitMapping[RhinoUnitSystem.Miles] = Units.Miles;
    _unitMapping[RhinoUnitSystem.Unset] = Units.Meters;
  }

  public string ConvertOrThrow(RhinoUnitSystem hostUnit)
  {
    if (_unitMapping.TryGetValue(hostUnit, out string value))
    {
      return value;
    }

    throw new SpeckleConversionException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
