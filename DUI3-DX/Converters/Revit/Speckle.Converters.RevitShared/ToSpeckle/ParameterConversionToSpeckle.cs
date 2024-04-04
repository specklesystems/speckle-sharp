using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(Parameter), 0)]
public class ParameterConversionToSpeckle : BaseConversionToSpeckle<DB.Parameter, SOBR.Parameter>
{
  private readonly ToSpeckleScalingService _scalingService;
  private readonly ParameterValueExtractor _valueExtractor;
  private readonly CachingService _cachingService;

  public ParameterConversionToSpeckle(
    ToSpeckleScalingService scalingService,
    ParameterValueExtractor valueExtractor,
    CachingService cachingService
  )
  {
    _scalingService = scalingService;
    _valueExtractor = valueExtractor;
    _cachingService = cachingService;
  }

  public override SOBR.Parameter RawConvert(DB.Parameter target)
  {
    string internalName = target.GetInternalName();
    ParameterToSpeckleData toSpeckleData = _cachingService.GetOrAdd(
      internalName,
      paramInternalName => ExtractParameterDataFromDocument(paramInternalName, target)
    );

    return toSpeckleData.GetParameterObjectWithValue(_valueExtractor.GetValue(target));
  }

  // POC: naming, I don't know if we need FromDocument, even if it is using it (but maybe it is not)
  private ParameterToSpeckleData ExtractParameterDataFromDocument(string paramInternalName, DB.Parameter parameter)
  {
    var definition = parameter.Definition;
    var newParamData = new ParameterToSpeckleData()
    {
      Definition = definition,
      InternalName = paramInternalName,
      IsReadOnly = parameter.IsReadOnly,
      IsShared = parameter.IsShared,
      Name = definition.Name,
      UnitType = definition.GetUnitTypeString(),
    };

    // POC: why is this specialisation needed? Could there be more?
    if (parameter.StorageType == StorageType.Double)
    {
      ForgeTypeId unitTypeId = parameter.GetUnitTypeId();
      newParamData.UnitsSymbol = unitTypeId.GetSymbol();
      newParamData.ApplicationUnits = unitTypeId.ToUniqueString();
    }

    return newParamData;
  }
}

/// <summary>
/// This struct is used when caching parameter definitions upon sending to avoid having to deep clone the parameter object
/// This is done because all the fields except the parameter value will change
/// </summary>
///
// POC: needed for caching but should it be a struct? We should have it in it's own file
internal struct ParameterToSpeckleData
{
  public string ApplicationUnits;
  public DB.Definition Definition;
  public string InternalName;
  public bool IsReadOnly;
  public bool IsShared;
  public string Name;
  public string? UnitsSymbol;
  public string UnitType;

  public readonly SOBR.Parameter GetParameterObjectWithValue(object? value)
  {
    return new SOBR.Parameter()
    {
      applicationInternalName = InternalName,
      applicationUnit = ApplicationUnits,
      isShared = IsShared,
      isReadOnly = IsReadOnly,
      name = Name,
      units = UnitsSymbol ?? "None",
      value = value
    };
  }
}
