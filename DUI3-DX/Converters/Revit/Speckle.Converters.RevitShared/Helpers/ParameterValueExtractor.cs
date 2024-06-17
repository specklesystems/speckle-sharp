using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: needs reviewing, it may be fine, not sure how open/closed it is
// really if we have to edit a switch statement...
// maybe also better as an extension method, but maybe is fine?
// POC: there are a lot of public methods here. Maybe consider consolodating

public class ParameterValueExtractor : IParameterValueExtractor
{
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly Dictionary<string, HashSet<RevitBuiltInParameter>> _uniqueIdToUsedParameterSetMap = new();

  public ParameterValueExtractor(IScalingServiceToSpeckle scalingService)
  {
    _scalingService = scalingService;
  }

  public object? GetValue(IRevitParameter parameter)
  {
    if (!parameter.HasValue)
    {
      return null;
    }

    return parameter.StorageType switch
    {
      RevitStorageType.Double => GetValueAsDouble(parameter),
      RevitStorageType.Integer => GetValueAsInt(parameter),
      RevitStorageType.String => GetValueAsString(parameter),
      RevitStorageType.ElementId => GetValueAsElementId(parameter)?.ToString(),
      RevitStorageType.None
      or _
        => throw new SpeckleConversionException($"Unsupported parameter storage type {parameter.StorageType}")
    };
  }

  public double GetValueAsDouble(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    if (!TryGetValueAsDouble(element, builtInParameter, out double? value))
    {
      throw new SpeckleConversionException($"Failed to get {builtInParameter} as double.");
    }

    return value.Value; // If TryGet returns true, we succeeded in obtaining the value, and it will not be null.
  }

  public bool TryGetValueAsDouble(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    [NotNullWhen(true)] out double? value
  )
  {
    var number = GetValueGeneric<double?>(
      element,
      builtInParameter,
      RevitStorageType.Double,
      (parameter) => _scalingService.Scale(parameter.AsDouble(), parameter.GetUnitTypeId())
    );
    if (number.HasValue)
    {
      value = number.Value;
      return true;
    }

    value = default;
    return false;
  }

  private double? GetValueAsDouble(IRevitParameter parameter)
  {
    return GetValueGeneric<double?>(
      parameter,
      RevitStorageType.Double,
      (parameter) => _scalingService.Scale(parameter.AsDouble(), parameter.GetUnitTypeId())
    );
  }

  public int GetValueAsInt(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    return GetValueGeneric<int?>(
        element,
        builtInParameter,
        RevitStorageType.Integer,
        (parameter) => parameter.AsInteger()
      )
      ?? throw new SpeckleConversionException(
        $"Expected int but got null for property {builtInParameter} on element of type {element.GetType()}"
      );
  }

  private int? GetValueAsInt(IRevitParameter parameter)
  {
    return GetValueGeneric<int?>(parameter, RevitStorageType.Integer, (parameter) => parameter.AsInteger());
  }

  public bool? GetValueAsBool(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    var intVal = GetValueGeneric<int?>(
      element,
      builtInParameter,
      RevitStorageType.Integer,
      (parameter) => parameter.AsInteger()
    );

    return intVal.HasValue ? Convert.ToBoolean(intVal.Value) : null;
  }

  public string? GetValueAsString(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    return GetValueGeneric(element, builtInParameter, RevitStorageType.String, (parameter) => parameter.AsString());
  }

  private string? GetValueAsString(IRevitParameter parameter)
  {
    return GetValueGeneric(parameter, RevitStorageType.String, (parameter) => parameter.AsString());
  }

  public IRevitElementId GetValueAsElementId(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    if (TryGetValueAsElementId(element, builtInParameter, out var elementId))
    {
      return elementId;
    }
    throw new SpeckleConversionException(
      $"Failed to get {builtInParameter} on element of type {element.GetType()} as ElementId"
    );
  }

  public bool TryGetValueAsElementId(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    [NotNullWhen(true)] out IRevitElementId? elementId
  )
  {
    var generic = GetValueGeneric(
      element,
      builtInParameter,
      RevitStorageType.ElementId,
      (parameter) => parameter.AsElementId()
    );
    if (generic is not null)
    {
      elementId = generic;
      return true;
    }

    elementId = null;
    return false;
  }

  public IRevitElementId? GetValueAsElementId(IRevitParameter parameter)
  {
    return GetValueGeneric(parameter, RevitStorageType.ElementId, (p) => p.AsElementId());
  }

  public IRevitLevel GetValueAsRevitLevel(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    if (!TryGetValueAsElementId(element, builtInParameter, out var elementId))
    {
      throw new SpeckleConversionException();
    }

    var paramElement = element.Document.GetElement(elementId);
    return (paramElement?.ToLevel()).NotNull();
  }

  public bool TryGetValueAsRevitLevel(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    [NotNullWhen(true)] out IRevitLevel? revitLevel
  )
  {
    if (!TryGetValueAsElementId(element, builtInParameter, out var elementId))
    {
      revitLevel = null;
      return false;
    }

    var paramElement = element.Document.GetElement(elementId);
    revitLevel = paramElement?.ToLevel();
    return revitLevel is not null;
  }

  private TResult? GetValueGeneric<TResult>(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    RevitStorageType expectedStorageType,
    Func<IRevitParameter, TResult> getParamValue
  )
  {
    if (
      !_uniqueIdToUsedParameterSetMap.TryGetValue(element.UniqueId, out HashSet<RevitBuiltInParameter> usedParameters)
    )
    {
      usedParameters = new();
      _uniqueIdToUsedParameterSetMap[element.UniqueId] = usedParameters;
    }
    usedParameters.Add(builtInParameter);
    var parameter = element.GetParameter(builtInParameter);
    if (parameter is null)
    {
      return default;
    }
    return GetValueGeneric(parameter, expectedStorageType, getParamValue);
  }

  private TResult? GetValueGeneric<TResult>(
    IRevitParameter parameter,
    RevitStorageType expectedStorageType,
    Func<IRevitParameter, TResult> getParamValue
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

  public Dictionary<string, IRevitParameter> GetAllRemainingParams(IRevitElement revitElement)
  {
    var allParams = new Dictionary<string, IRevitParameter>();
    AddElementParamsToDict(revitElement, allParams);

    return allParams;
  }

  private void AddElementParamsToDict(IRevitElement element, Dictionary<string, IRevitParameter> paramDict)
  {
    _uniqueIdToUsedParameterSetMap.TryGetValue(element.UniqueId, out HashSet<RevitBuiltInParameter>? usedParameters);

    using var parameters = element.Parameters;
    foreach (IRevitParameter param in parameters)
    {
      var internalName = param.GetInternalName();
      if (paramDict.ContainsKey(internalName))
      {
        continue;
      }

      var bip = param.GetBuiltInParameter();
      if (bip is not null && (usedParameters?.Contains(bip.Value) ?? false))
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
