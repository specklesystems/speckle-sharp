using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.Services;

// POC: feels like this is a context thing and we should be calculating this occasionally?
// needs some thought as to how it could be be done, could leave as is for now
public sealed class ScalingServiceToSpeckle
{
  private readonly double _defaultLengthConversionFactor;

  // POC: this seems like the reverse relationship
  public ScalingServiceToSpeckle(RevitConversionContextStack contextStack)
  {
    // POC: this is accurate for the current context stack
    Units documentUnits = contextStack.Current.Document.Document.GetUnits();
    FormatOptions formatOptions = documentUnits.GetFormatOptions(SpecTypeId.Length);
    var lengthUnitsTypeId = formatOptions.GetUnitTypeId();
    _defaultLengthConversionFactor = ScaleStatic(1, lengthUnitsTypeId);
  }

  public double ScaleLength(double length) => length * _defaultLengthConversionFactor;

  // POC: not sure about this???
  public double Scale(double value, ForgeTypeId forgeTypeId)
  {
    return ScaleStatic(value, forgeTypeId);
  }

  // POC: not sure why this is needed???
  private static double ScaleStatic(double value, ForgeTypeId forgeTypeId)
  {
    return UnitUtils.ConvertFromInternalUnits(value, forgeTypeId);
  }
}
