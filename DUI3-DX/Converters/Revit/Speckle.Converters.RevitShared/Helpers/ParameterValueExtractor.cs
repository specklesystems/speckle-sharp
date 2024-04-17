using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: needs reviewing, it may be fine, not sure how open/closed it is
// really if we have to edit a switch statement...
// maybe also better as an extension method, but maybe is fine?
public class ParameterValueExtractor
{
  private readonly ToSpeckleScalingService _scalingService;
  private readonly Dictionary<string, HashSet<BuiltInParameter>> _uniqueIdToUsedParameterSetMap = new();

  public ParameterValueExtractor(ToSpeckleScalingService scalingService)
  {
    _scalingService = scalingService;
  }

  public object? GetValue(Parameter parameter)
  {
    if (!parameter.HasValue)
    {
      return null;
    }

    return parameter.StorageType switch
    {
      StorageType.Double => GetValueAsDouble(parameter),
      StorageType.Integer => GetValueAsInt(parameter),
      StorageType.String => GetValueAsString(parameter),
      StorageType.ElementId => GetValueAsElementId(parameter)?.ToString(),
      StorageType.None
      or _
        => throw new SpeckleConversionException($"Unsupported parameter storage type {parameter.StorageType}")
    };
  }

  public double? GetValueAsDouble(Element element, BuiltInParameter builtInParameter)
  {
    return GetValueGeneric<double?>(
      element,
      builtInParameter,
      StorageType.Double,
      (parameter) => _scalingService.Scale(parameter.AsDouble(), parameter.GetUnitTypeId())
    );
  }

  private double? GetValueAsDouble(Parameter parameter)
  {
    return GetValueGeneric<double?>(
      parameter,
      StorageType.Double,
      (parameter) => _scalingService.Scale(parameter.AsDouble(), parameter.GetUnitTypeId())
    );
  }

  public int? GetValueAsInt(Element element, BuiltInParameter builtInParameter)
  {
    return GetValueGeneric<int?>(element, builtInParameter, StorageType.Integer, (parameter) => parameter.AsInteger());
  }

  private int? GetValueAsInt(Parameter parameter)
  {
    return GetValueGeneric<int?>(parameter, StorageType.Integer, (parameter) => parameter.AsInteger());
  }

  public bool? GetValueAsBool(Element element, BuiltInParameter builtInParameter)
  {
    var intVal = GetValueGeneric<int?>(
      element,
      builtInParameter,
      StorageType.Integer,
      (parameter) => parameter.AsInteger()
    );

    return intVal.HasValue ? Convert.ToBoolean(intVal.Value) : null;
  }

  public string? GetValueAsString(Element element, BuiltInParameter builtInParameter)
  {
    return GetValueGeneric(element, builtInParameter, StorageType.String, (parameter) => parameter.AsString());
  }

  private string? GetValueAsString(Parameter parameter)
  {
    return GetValueGeneric(parameter, StorageType.String, (parameter) => parameter.AsString());
  }

  public ElementId? GetValueAsElementId(Element element, BuiltInParameter builtInParameter)
  {
    return GetValueGeneric(element, builtInParameter, StorageType.ElementId, (parameter) => parameter.AsElementId());
  }

  public ElementId? GetValueAsElementId(Parameter parameter)
  {
    return GetValueGeneric(parameter, StorageType.ElementId, (parameter) => parameter.AsElementId());
  }

  public T GetValueAsDocumentObject<T>(Element element, BuiltInParameter builtInParameter)
    where T : class
  {
    ElementId? elementId = GetValueAsElementId(element, builtInParameter);
    Element paramElement = element.Document.GetElement(elementId);
    return paramElement as T
      ?? throw new SpeckleConversionException(
        $"Unable to cast retrieved element of type {paramElement.GetType()} to an element of type {typeof(T)}"
      );
  }

  private TResult? GetValueGeneric<TResult>(
    Element element,
    BuiltInParameter builtInParameter,
    StorageType expectedStorageType,
    Func<DB.Parameter, TResult> getParamValue
  )
  {
    if (!_uniqueIdToUsedParameterSetMap.TryGetValue(element.UniqueId, out HashSet<BuiltInParameter> usedParameters))
    {
      usedParameters = new();
      _uniqueIdToUsedParameterSetMap[element.UniqueId] = usedParameters;
    }
    usedParameters.Add(builtInParameter);
    var parameter = element.get_Parameter(builtInParameter);
    return GetValueGeneric(parameter, expectedStorageType, getParamValue);
  }

  private TResult? GetValueGeneric<TResult>(
    Parameter parameter,
    StorageType expectedStorageType,
    Func<DB.Parameter, TResult> getParamValue
  )
  {
    if (parameter == null || !parameter.HasValue)
    {
      return default;
    }

    if (parameter.StorageType != expectedStorageType)
    {
      throw new SpeckleConversionException(
        $"Expected parameter of storage type {expectedStorageType} but got parameter of storage type {parameter.StorageType}"
      );
    }

    return getParamValue(parameter);
  }

  public Dictionary<string, Parameter> GetAllRemainingParams(DB.Element revitElement)
  {
    var allParams = new Dictionary<string, Parameter>();
    AddElementParamsToDict(revitElement, allParams);

    return allParams;
  }

  private void AddElementParamsToDict(DB.Element element, Dictionary<string, Parameter> paramDict)
  {
    _uniqueIdToUsedParameterSetMap.TryGetValue(element.UniqueId, out HashSet<BuiltInParameter>? usedParameters);

    using var parameters = element.Parameters;
    foreach (DB.Parameter param in parameters)
    {
      var internalName = param.GetInternalName();
      if (paramDict.ContainsKey(internalName))
      {
        continue;
      }

      if (param.GetBuiltInParameter() is BuiltInParameter bip && (usedParameters?.Contains(bip) ?? false))
      {
        continue;
      }

      paramDict[internalName] = param;
    }
  }

  public void RemoveUniqueId(string uniqueId)
  {
    _uniqueIdToUsedParameterSetMap.Remove(uniqueId);
  }
}
