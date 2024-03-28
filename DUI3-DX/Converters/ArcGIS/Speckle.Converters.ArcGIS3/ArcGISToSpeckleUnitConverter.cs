using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging; // POC: boy do I think this is the wrong place for SpeckleException!

namespace Speckle.Converters.ArcGIS3;

public class ArcGISToSpeckleUnitConverter : IHostToSpeckleUnitConverter<Unit>
{
  private readonly Dictionary<string, string> _unitMapping = new();

  public ArcGISToSpeckleUnitConverter()
  {
    // POC: we should have a unit test to confirm these are as expected and don't change
    //_unitMapping[LinearUnit.] = Units.Meters;
    _unitMapping[LinearUnit.Millimeters.Name] = Units.Millimeters;
    _unitMapping[LinearUnit.Centimeters.Name] = Units.Centimeters;
    _unitMapping[LinearUnit.Meters.Name] = Units.Meters;
    _unitMapping[LinearUnit.Kilometers.Name] = Units.Kilometers;
    _unitMapping[LinearUnit.Inches.Name] = Units.Inches;
    _unitMapping[LinearUnit.Feet.Name] = Units.Feet;
    _unitMapping[LinearUnit.Yards.Name] = Units.Yards;
    _unitMapping[LinearUnit.Miles.Name] = Units.Miles;
    //_unitMapping[LinearUnit.Decimeters] = Units.;
    //_unitMapping[LinearUnit.NauticalMiles] = Units.;
  }

  public string ConvertOrThrow(Unit hostUnit)
  {
    var linearUnit = LinearUnit.CreateLinearUnit(hostUnit.Wkt).Name;

    if (_unitMapping.TryGetValue(linearUnit, out string value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
