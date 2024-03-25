using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Parameter), 0)]
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
    string internalName = GetParamInternalName(target);
    ParameterToSpeckleData toSpeckleData = _cachingService.GetOrAdd(
      internalName,
      paramInternalName => ExtractParameterDataFromDocument(paramInternalName, target)
    );

    return toSpeckleData.GetParameterObjectWithValue(_valueExtractor.GetValue(target));
  }

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
    if (parameter.StorageType == StorageType.Double)
    {
      ForgeTypeId unitTypeId = parameter.GetUnitTypeId();
      newParamData.UnitsSymbol = unitTypeId.GetSymbol();
      newParamData.ApplicationUnits = unitTypeId.ToUniqueString();
    }
    return newParamData;
  }

  //Shared parameters use a GUID to be uniquely identified
  //Other parameters use a BuiltInParameter enum
  private static string GetParamInternalName(DB.Parameter rp)
  {
    if (rp.IsShared)
    {
      return rp.GUID.ToString();
    }
    else
    {
      var def = rp.Definition as InternalDefinition;
      if (def.BuiltInParameter == BuiltInParameter.INVALID)
      {
        return def.Name;
      }

      return def.BuiltInParameter.ToString();
    }
  }
}

/// <summary>
/// This struct is used when caching parameter definitions upon sending to avoid having to deep clone the parameter object
/// This is done because all the fields except the parameter value will change
/// </summary>
internal struct ParameterToSpeckleData
{
  public string ApplicationUnits;
  public DB.Definition Definition;
  public string InternalName;
  public bool IsReadOnly;
  public bool IsShared;
  public bool IsTypeParameter;
  public string Name;
  public string UnitsSymbol;
  public string UnitType;

  public readonly SOBR.Parameter GetParameterObjectWithValue(object? value)
  {
    return new SOBR.Parameter()
    {
      applicationInternalName = InternalName,
      applicationUnit = ApplicationUnits,
      isShared = IsShared,
      isReadOnly = IsReadOnly,
      isTypeParameter = IsTypeParameter,
      name = Name,
      units = UnitsSymbol,
      value = value
    };
  }
}
