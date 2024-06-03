using Speckle.Converters.RevitShared.Helpers;
using Speckle.InterfaceGenerator;
using Speckle.Revit2023.Api;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Converters.RevitShared.Services;

// POC: feels like this is a context thing and we should be calculating this occasionally?
// needs some thought as to how it could be be done, could leave as is for now
[GenerateAutoInterface]
public sealed class ScalingServiceToSpeckle : IScalingServiceToSpeckle
{
  private readonly double _defaultLengthConversionFactor;
  private readonly IRevitUnitUtils _revitUnitUtils;

  // POC: this seems like the reverse relationship
  public ScalingServiceToSpeckle(IRevitConversionContextStack contextStack, IRevitUnitUtils revitUnitUtils)
  {
    _revitUnitUtils = revitUnitUtils;
    // POC: this is accurate for the current context stack
    var documentUnits = contextStack.Current.Document.GetUnits();
    var formatOptions = documentUnits.GetFormatOptions(RevitSpecTypeId.Length);
    var lengthUnitsTypeId = formatOptions.GetUnitTypeId();
    _defaultLengthConversionFactor = _revitUnitUtils.ConvertFromInternalUnits(1, lengthUnitsTypeId);
  }

  // POC: throughout Revit conversions there's lots of comparison to check the units are valid
  // atm we seem to be expecting that this is correct and that the scaling will be fixed for the duration
  // of a conversion, but...  I have some concerns that the units and the conversion may change, for instance, for linked documents?
  // this needs to be considered and perahps scaling should be part of the context, or at least part of the IRevitConversionContextStack
  public double ScaleLength(double length) => length * _defaultLengthConversionFactor;

  // POC: not sure about this???
  public double Scale(double value, IRevitForgeTypeId forgeTypeId)
  {
    return _revitUnitUtils.ConvertFromInternalUnits(value, forgeTypeId);
  }
  
  public double Scale(double value, DB.ForgeTypeId forgeTypeId)
  {
    return  DB.UnitUtils.ConvertFromInternalUnits(value, forgeTypeId);
  }
}
