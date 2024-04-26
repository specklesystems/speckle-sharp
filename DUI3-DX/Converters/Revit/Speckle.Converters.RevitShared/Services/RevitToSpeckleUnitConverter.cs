using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.Converters.RevitShared.Services;

public sealed class RevitToSpeckleUnitConverter : IHostToSpeckleUnitConverter<DB.ForgeTypeId>
{
  private readonly Dictionary<DB.ForgeTypeId, string> _unitMapping = new();

  public RevitToSpeckleUnitConverter()
  {
    _unitMapping[DB.UnitTypeId.Millimeters] = Units.Millimeters;
    _unitMapping[DB.UnitTypeId.Centimeters] = Units.Centimeters;
    _unitMapping[DB.UnitTypeId.Meters] = Units.Meters;
    _unitMapping[DB.UnitTypeId.Inches] = Units.Inches;
    _unitMapping[DB.UnitTypeId.Feet] = Units.Feet;
  }

  // POC: maybe just convert, it's not a Try method
  public string ConvertOrThrow(DB.ForgeTypeId hostUnit)
  {
    if (_unitMapping.TryGetValue(hostUnit, out string value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
