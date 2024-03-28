using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.Helpers;

public sealed class ParameterObjectAssigner
{
  private readonly IRawConversion<DB.Parameter, SOBR.Parameter> _paramConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public ParameterObjectAssigner(
    IRawConversion<DB.Parameter, SOBR.Parameter> paramConverter,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _paramConverter = paramConverter;
    _parameterValueExtractor = parameterValueExtractor;
  }

  public void AssignParametersToBase(Element target, Base @base)
  {
    Dictionary<string, DB.Parameter> allParams = _parameterValueExtractor.GetAllRemainingParams(target);
    Base paramBase = new();
    //sort by key
    foreach (var kv in allParams.OrderBy(x => x.Key))
    {
      try
      {
        paramBase[kv.Key] = _paramConverter.RawConvert(kv.Value);
      }
      catch (InvalidPropNameException)
      {
        //ignore
      }
      catch (SpeckleException ex)
      {
        SpeckleLog.Logger.Warning(ex, "Error thrown when trying to set property named {propName}", kv.Key);
      }
    }

    if (paramBase.GetMembers(DynamicBaseMemberType.Dynamic).Count > 0)
    {
      @base["parameters"] = paramBase;
    }
  }
}
