using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Services;

public sealed class RevitToSpeckleUnitConverter : IHostToSpeckleUnitConverter<IRevitForgeTypeId>
{
  private readonly Dictionary<IRevitForgeTypeId, string> _unitMapping = new();

  public RevitToSpeckleUnitConverter(IRevitUnitUtils revitUnitUtils)
  {
    _unitMapping[revitUnitUtils.Millimeters] = Units.Millimeters;
    _unitMapping[revitUnitUtils.Centimeters] = Units.Centimeters;
    _unitMapping[revitUnitUtils.Meters] = Units.Meters;
    _unitMapping[revitUnitUtils.MetersCentimeters] = Units.Meters;
    _unitMapping[revitUnitUtils.Inches] = Units.Inches;
    _unitMapping[revitUnitUtils.FractionalInches] = Units.Inches;
    _unitMapping[revitUnitUtils.Feet] = Units.Feet;
    _unitMapping[revitUnitUtils.FeetFractionalInches] = Units.Feet;
  }

  // POC: maybe just convert, it's not a Try method
  public string ConvertOrThrow(IRevitForgeTypeId hostUnit)
  {
    if (_unitMapping.TryGetValue(hostUnit, out string value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
