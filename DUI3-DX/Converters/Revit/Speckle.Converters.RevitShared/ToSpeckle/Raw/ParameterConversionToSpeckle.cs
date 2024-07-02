using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class ParameterConversionToSpeckle : ITypedConverter<Parameter, SOBR.Parameter>
{
  private readonly ParameterValueExtractor _valueExtractor;

  public ParameterConversionToSpeckle(ParameterValueExtractor valueExtractor)
  {
    _valueExtractor = valueExtractor;
  }

  public SOBR.Parameter Convert(Parameter target)
  {
    string internalName = target.GetInternalName();
    ForgeTypeId? unitTypeId = null;
    if (target.StorageType is StorageType.Double)
    {
      // according to the api documentation, this method will throw if the storage type is not a VALUE type
      // however, I've found that it will still throw if StorageType == StorageType.Integer
      unitTypeId = target.GetUnitTypeId();
    }
    Definition definition = target.Definition;

    return new SOBR.Parameter()
    {
      applicationInternalName = internalName,
      applicationUnit = unitTypeId?.ToUniqueString() ?? "None",
      isShared = target.IsShared,
      isReadOnly = target.IsReadOnly,
      name = definition.Name,
      units = unitTypeId?.GetSymbol() ?? "None",
      value = _valueExtractor.GetValue(target)
    };
  }
}
