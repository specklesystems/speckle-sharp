using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: rationalise whether this and ParameterObjectBuilder are sufficiently different??
// did it go away?
public sealed class ParameterObjectAssigner
{
  private readonly ITypedConverter<Parameter, SOBR.Parameter> _paramConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public ParameterObjectAssigner(
    ITypedConverter<Parameter, SOBR.Parameter> paramConverter,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _paramConverter = paramConverter;
    _parameterValueExtractor = parameterValueExtractor;
  }

  public void AssignParametersToBase(Element target, Base @base)
  {
    Dictionary<string, Parameter> instanceParameters = _parameterValueExtractor.GetAllRemainingParams(target);
    ElementId elementId = target.GetTypeId();

    Base paramBase = new();
    AssignSpeckleParamToBaseObject(instanceParameters, paramBase);

    // POC: Some elements can have an invalid element type ID, I don't think we want to continue here.
    if (elementId != ElementId.InvalidElementId && target is not Level) //ignore type props of levels..!
    {
      var elementType = target.Document.GetElement(elementId);
      // I don't think we should be adding the type parameters to the object like this
      Dictionary<string, Parameter> typeParameters = _parameterValueExtractor.GetAllRemainingParams(elementType);
      AssignSpeckleParamToBaseObject(typeParameters, paramBase, true);
    }

    if (paramBase.GetMembers(DynamicBaseMemberType.Dynamic).Count > 0)
    {
      @base["parameters"] = paramBase;
    }
  }

  private void AssignSpeckleParamToBaseObject(
    IEnumerable<KeyValuePair<string, Parameter>> parameters,
    Base paramBase,
    bool isTypeParameter = false
  )
  {
    //sort by key
    foreach (var kv in parameters.OrderBy(x => x.Key))
    {
      try
      {
        SOBR.Parameter speckleParam = _paramConverter.Convert(kv.Value);
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
        SpeckleLog.Logger.Warning(ex, "Error thrown when trying to set property named {propName}", kv.Key);
      }
    }
  }
}
