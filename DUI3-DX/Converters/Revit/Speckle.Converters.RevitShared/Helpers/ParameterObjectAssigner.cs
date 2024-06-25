using Microsoft.Extensions.Logging;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: rationalise whether this and ParameterObjectBuilder are sufficiently different??
// did it go away?
[GenerateAutoInterface]
public sealed class ParameterObjectAssigner : IParameterObjectAssigner
{
  private readonly ITypedConverter<IRevitParameter, SOBR.Parameter> _paramConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly IRevitElementIdUtils _revitElementIdUtils;
  private readonly ILogger<ParameterObjectAssigner> _logger;

  public ParameterObjectAssigner(
    ITypedConverter<IRevitParameter, SOBR.Parameter> paramConverter,
    IParameterValueExtractor parameterValueExtractor,
    IRevitElementIdUtils revitElementIdUtils, ILogger<ParameterObjectAssigner> logger)
  {
    _paramConverter = paramConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _revitElementIdUtils = revitElementIdUtils;
    _logger = logger;
  }

  public void AssignParametersToBase(IRevitElement target, Base @base)
  {
    var instanceParameters = _parameterValueExtractor.GetAllRemainingParams(target);
    IRevitElementId elementId = target.GetTypeId();

    Base paramBase = new();
    AssignSpeckleParamToBaseObject(instanceParameters, paramBase);

    // POC: Some elements can have an invalid element type ID, I don't think we want to continue here.
    if (elementId.IntegerValue != _revitElementIdUtils.InvalidElementId.IntegerValue && target is not SOBE.Level) //ignore type props of levels..!
    {
      var elementType = target.Document.GetElement(elementId).NotNull();
      // I don't think we should be adding the type parameters to the object like this
      var typeParameters = _parameterValueExtractor.GetAllRemainingParams(elementType);
      AssignSpeckleParamToBaseObject(typeParameters, paramBase, true);
    }

    if (paramBase.GetMembers(DynamicBaseMemberType.Dynamic).Count > 0)
    {
      @base["parameters"] = paramBase;
    }
  }

  private void AssignSpeckleParamToBaseObject(
    IEnumerable<KeyValuePair<string, IRevitParameter>> parameters,
    Base paramBase,
    bool isTypeParameter = false
  )
  {
    //sort by key
    foreach (var kv in parameters.OrderBy(x => x.Key))
    {
      try
      {
        var speckleParam = _paramConverter.Convert(kv.Value);
        speckleParam.isTypeParameter = isTypeParameter;
        paramBase[kv.Key] = speckleParam;
      }
      // POC swallow and continue seems bad?
      // maybe hoover these into one exception or into our reporting strategy
      catch (InvalidPropNameException)
      {
        //ignore
      }
      // POC swallow and continue seems bad?
      // maybe hoover these into one exception or into our reporting strategy
      catch (SpeckleConversionException ex)
      {
        _logger.LogWarning(ex, $"Error thrown when trying to set property named {kv.Key}");
      }
    }
  }
}
