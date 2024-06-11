using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023;

public class ParameterConversionToSpeckle : ITypedConverter<IRevitParameter, SOBR.Parameter>
{
  private readonly IParameterValueExtractor _valueExtractor;
  private readonly IRevitFormatOptionsUtils _revitFormatOptionsUtils;

  public ParameterConversionToSpeckle(
    IParameterValueExtractor valueExtractor,
    IRevitFormatOptionsUtils revitFormatOptionsUtils
  )
  {
    _valueExtractor = valueExtractor;
    _revitFormatOptionsUtils = revitFormatOptionsUtils;
  }

  public SOBR.Parameter Convert(IRevitParameter target)
  {
    string internalName = target.GetInternalName();
    IRevitForgeTypeId? unitTypeId = null;
    if (target.StorageType is IRevitStorageType.Double)
    {
      // according to the api documentation, this method will throw if the storage type is not a VALUE type
      // however, I've found that it will still throw if StorageType == StorageType.Integer
      unitTypeId = target.GetUnitTypeId();
    }
    IRevitDefinition definition = target.Definition;

    return new SOBR.Parameter()
    {
      applicationInternalName = internalName,
      applicationUnit = unitTypeId?.ToUniqueString() ?? "None",
      isShared = target.IsShared,
      isReadOnly = target.IsReadOnly,
      name = definition.Name,
      units = unitTypeId?.GetSymbol(_revitFormatOptionsUtils) ?? "None",
      value = _valueExtractor.GetValue(target)
    };
  }
}
