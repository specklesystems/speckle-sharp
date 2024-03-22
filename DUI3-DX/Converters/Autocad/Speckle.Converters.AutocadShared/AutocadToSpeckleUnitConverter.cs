using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging; // POC: boy do I think this is the wrong place for SpeckleException!

namespace Speckle.Converters.AutocadShared;

public class AutocadToSpeckleUnitConverter : IHostToSpeckleUnitConverter<UnitsValue>
{
  private readonly Dictionary<UnitsValue, string> _unitMapping = new();

  public AutocadToSpeckleUnitConverter()
  {
    // POC: we should have a unit test to confirm these are as expected and don't change
    _unitMapping[UnitsValue.Undefined] = Units.Meters;
    _unitMapping[UnitsValue.Millimeters] = Units.Millimeters;
    _unitMapping[UnitsValue.Centimeters] = Units.Centimeters;
    _unitMapping[UnitsValue.Meters] = Units.Meters;
    _unitMapping[UnitsValue.Kilometers] = Units.Kilometers;
    _unitMapping[UnitsValue.Inches] = Units.Inches;
    _unitMapping[UnitsValue.Feet] = Units.Feet;
    _unitMapping[UnitsValue.Yards] = Units.Yards;
    _unitMapping[UnitsValue.Miles] = Units.Miles;
  }

  public string ConvertOrThrow(UnitsValue hostUnit)
  {
    if (_unitMapping.TryGetValue(hostUnit, out string value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
