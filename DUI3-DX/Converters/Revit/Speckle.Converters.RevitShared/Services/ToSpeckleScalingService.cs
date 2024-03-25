using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.Services;

public sealed class ToSpeckleScalingService
{
  private readonly double _defaultLengthConversionFactor;

  public ToSpeckleScalingService(RevitConversionContextStack contextStack)
  {
    Units documentUnits = contextStack.Current.Document.Document.GetUnits();
    FormatOptions formatOptions = documentUnits.GetFormatOptions(SpecTypeId.Length);
    var lengthUnitsTypeId = formatOptions.GetUnitTypeId();
    _defaultLengthConversionFactor = ScaleStatic(1, lengthUnitsTypeId);
  }

  public string SpeckleUnits { get; }

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
