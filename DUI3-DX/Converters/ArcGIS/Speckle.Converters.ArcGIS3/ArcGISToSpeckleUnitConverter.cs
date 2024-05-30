using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging; // POC: boy do I think this is the wrong place for SpeckleException!

namespace Speckle.Converters.ArcGIS3;

public class ArcGISToSpeckleUnitConverter : IHostToSpeckleUnitConverter<Unit>
{
  private readonly Dictionary<int, string> _unitMapping = new();

  private ArcGISToSpeckleUnitConverter()
  {
    // POC: we should have a unit test to confirm these are as expected and don't change
    // more units: https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8349.html
    _unitMapping[LinearUnit.Millimeters.FactoryCode] = Units.Millimeters;
    _unitMapping[LinearUnit.Centimeters.FactoryCode] = Units.Centimeters;
    _unitMapping[LinearUnit.Meters.FactoryCode] = Units.Meters;
    _unitMapping[LinearUnit.Kilometers.FactoryCode] = Units.Kilometers;
    _unitMapping[LinearUnit.Inches.FactoryCode] = Units.Inches;
    _unitMapping[LinearUnit.Feet.FactoryCode] = Units.Feet;
    _unitMapping[LinearUnit.Yards.FactoryCode] = Units.Yards;
    _unitMapping[LinearUnit.Miles.FactoryCode] = Units.Miles;
    _unitMapping[9003] = Units.USFeet;
  }

  public string ConvertOrThrow(Unit hostUnit)
  {
    int linearUnit = LinearUnit.CreateLinearUnit(hostUnit.Wkt).FactoryCode;

    if (_unitMapping.TryGetValue(linearUnit, out string? value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
