using System.Collections.Generic;
using System.Linq;
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
  private readonly IRawConversion<Parameter, SOBR.Parameter> _paramConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public ParameterObjectAssigner(
    IRawConversion<Parameter, SOBR.Parameter> paramConverter,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _paramConverter = paramConverter;
    _parameterValueExtractor = parameterValueExtractor;
  }

  public void AssignParametersToBase(Element target, Base @base)
  {
    Dictionary<string, Parameter> instanceParameters = _parameterValueExtractor.GetAllRemainingParams(target);
    var elementType = target.Document.GetElement(target.GetTypeId());

    Base paramBase = new();
    AssignSpeckleParamToBaseObject(instanceParameters, paramBase);

    if (target is not Level) //ignore type props of levels..!
    {
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
        SOBR.Parameter speckleParam = _paramConverter.RawConvert(kv.Value);
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
