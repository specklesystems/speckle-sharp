using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.Services;

public sealed class ToSpeckleScalingService
{
  private readonly double _defaultLengthConversionFactor;

  public ToSpeckleScalingService(RevitConversionContextStack contextStack)
  {
    var formatOptions = contextStack.Current.Document.Document.GetUnits().GetFormatOptions(SpecTypeId.Length);
    var lengthUnitsTypeId = formatOptions.GetUnitTypeId();
    _defaultLengthConversionFactor = ScaleStatic(1, lengthUnitsTypeId);
  }

  public double ScaleLength(double length) => length * _defaultLengthConversionFactor;

  public double Scale(double value, ForgeTypeId forgeTypeId)
  {
    return ScaleStatic(value, forgeTypeId);
  }

  private static double ScaleStatic(double value, ForgeTypeId forgeTypeId)
  {
    return UnitUtils.ConvertFromInternalUnits(value, forgeTypeId);
  }
}
