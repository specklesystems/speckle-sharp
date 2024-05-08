using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.Services;

// POC: feels like this is a context thing and we should be calculating this occasionally?
// needs some thought as to how it could be be done, could leave as is for now
public sealed class ScalingServiceToSpeckle
{
  private readonly double _defaultLengthConversionFactor;

  // POC: this seems like the reverse relationship
  public ScalingServiceToSpeckle(IRevitConversionContextStack contextStack)
  {
    // POC: this is accurate for the current context stack
    Units documentUnits = contextStack.Current.Document.GetUnits();
    FormatOptions formatOptions = documentUnits.GetFormatOptions(SpecTypeId.Length);
    var lengthUnitsTypeId = formatOptions.GetUnitTypeId();
    _defaultLengthConversionFactor = ScaleStatic(1, lengthUnitsTypeId);
  }

  // POC: throughout Revit conversions there's lots of comparison to check the units are valid
  // atm we seem to be expecting that this is correct and that the scaling will be fixed for the duration
  // of a conversion, but...  I have some concerns that the units and the conversion may change, for instance, for linked documents?
  // this needs to be considered and perahps scaling should be part of the context, or at least part of the IRevitConversionContextStack
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
