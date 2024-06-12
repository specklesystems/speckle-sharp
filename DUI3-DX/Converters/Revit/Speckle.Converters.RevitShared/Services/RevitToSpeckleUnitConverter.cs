using Speckle.Converters.Common;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Services;

public sealed class RevitToSpeckleUnitConverter : IHostToSpeckleUnitConverter<IRevitForgeTypeId>
{
  private readonly Dictionary<string, string> _unitMapping = new();

  public RevitToSpeckleUnitConverter(IRevitUnitUtils revitUnitUtils)
  {
    _unitMapping[revitUnitUtils.Millimeters.TypeId] = Units.Millimeters;
    _unitMapping[revitUnitUtils.Centimeters.TypeId] = Units.Centimeters;
    _unitMapping[revitUnitUtils.Meters.TypeId] = Units.Meters;
    _unitMapping[revitUnitUtils.MetersCentimeters.TypeId] = Units.Meters;
    _unitMapping[revitUnitUtils.Inches.TypeId] = Units.Inches;
    _unitMapping[revitUnitUtils.FractionalInches.TypeId] = Units.Inches;
    _unitMapping[revitUnitUtils.Feet.TypeId] = Units.Feet;
    _unitMapping[revitUnitUtils.FeetFractionalInches.TypeId] = Units.Feet;
  }

  // POC: maybe just convert, it's not a Try method
  public string ConvertOrThrow(IRevitForgeTypeId hostUnit)
  {
    if (_unitMapping.TryGetValue(hostUnit.TypeId, out string value))
    {
      return value;
    }

    // POC: probably would prefer something more specific
    throw new SpeckleException($"The Unit System \"{hostUnit}\" is unsupported.");
  }
}
