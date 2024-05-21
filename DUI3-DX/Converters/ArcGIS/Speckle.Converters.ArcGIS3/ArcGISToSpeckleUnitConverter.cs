using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging; // POC: boy do I think this is the wrong place for SpeckleException!

namespace Speckle.Converters.ArcGIS3;

public class ArcGISToSpeckleUnitConverter : IHostToSpeckleUnitConverter<Unit>
{
  private static readonly IReadOnlyDictionary<string, string> s_unitMapping = Create();

  private static IReadOnlyDictionary<string, string> Create()
  {
    var dict = new Dictionary<string, string>();
    // POC: we should have a unit test to confirm these are as expected and don't change
    //_unitMapping[LinearUnit.] = Units.Meters;
    dict[LinearUnit.Millimeters.Name] = Units.Millimeters;
    dict[LinearUnit.Centimeters.Name] = Units.Centimeters;
    dict[LinearUnit.Meters.Name] = Units.Meters;
    dict[LinearUnit.Kilometers.Name] = Units.Kilometers;
    dict[LinearUnit.Inches.Name] = Units.Inches;
    dict[LinearUnit.Feet.Name] = Units.Feet;
    dict[LinearUnit.Yards.Name] = Units.Yards;
    dict[LinearUnit.Miles.Name] = Units.Miles;
    //_unitMapping[LinearUnit.Decimeters] = Units.;
    //_unitMapping[LinearUnit.NauticalMiles] = Units.;
    return dict;
  }

  public string ConvertOrThrow(Unit hostUnit)
  {
    var linearUnit = LinearUnit.CreateLinearUnit(hostUnit.Wkt).Name;

    if (s_unitMapping.TryGetValue(linearUnit, out string? value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
