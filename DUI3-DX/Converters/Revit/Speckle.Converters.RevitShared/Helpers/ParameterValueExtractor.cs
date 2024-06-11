using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Services;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: needs reviewing, it may be fine, not sure how open/closed it is
// really if we have to edit a switch statement...
// maybe also better as an extension method, but maybe is fine?
// POC: there are a lot of public methods here. Maybe consider consolodating
[GenerateAutoInterface]
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
      IRevitStorageType.Double => GetValueAsDouble(parameter),
      IRevitStorageType.Integer => GetValueAsInt(parameter),
      IRevitStorageType.String => GetValueAsString(parameter),
      IRevitStorageType.ElementId => GetValueAsElementId(parameter)?.ToString(),
      IRevitStorageType.None
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
      IRevitStorageType.Double,
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
      IRevitStorageType.Double,
      (parameter) => _scalingService.Scale(parameter.AsDouble(), parameter.GetUnitTypeId())
    );
  }

  public int GetValueAsInt(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    return GetValueGeneric<int?>(
        element,
        builtInParameter,
        IRevitStorageType.Integer,
        (parameter) => parameter.AsInteger()
      )
      ?? throw new SpeckleConversionException(
        $"Expected int but got null for property {builtInParameter} on element of type {element.GetType()}"
      );
  }

  private int? GetValueAsInt(IRevitParameter parameter)
  {
    return GetValueGeneric<int?>(parameter, IRevitStorageType.Integer, (parameter) => parameter.AsInteger());
  }

  public bool? GetValueAsBool(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    var intVal = GetValueGeneric<int?>(
      element,
      builtInParameter,
      IRevitStorageType.Integer,
      (parameter) => parameter.AsInteger()
    );

    return intVal.HasValue ? Convert.ToBoolean(intVal.Value) : null;
  }

  public string? GetValueAsString(IRevitElement element, RevitBuiltInParameter builtInParameter)
  {
    return GetValueGeneric(element, builtInParameter, IRevitStorageType.String, (parameter) => parameter.AsString());
  }

  private string? GetValueAsString(IRevitParameter parameter)
  {
    return GetValueGeneric(parameter, IRevitStorageType.String, (parameter) => parameter.AsString());
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
    if (
      GetValueGeneric(element, builtInParameter, IRevitStorageType.ElementId, (parameter) => parameter.AsElementId())
      is IRevitElementId elementIdNotNull
    )
    {
      elementId = elementIdNotNull;
      return true;
    }

    elementId = null;
    return false;
  }

  public IRevitElementId? GetValueAsElementId(IRevitParameter parameter)
  {
    return GetValueGeneric(parameter, IRevitStorageType.ElementId, (parameter) => parameter.AsElementId());
  }

  public bool TryGetValueAsDocumentObject<T>(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    [NotNullWhen(true)] out T? value
  )
  {
    if (!TryGetValueAsElementId(element, builtInParameter, out var elementId))
    {
      value = default;
      return false;
    }

    IRevitElement paramElement = element.Document.GetElement(elementId);
    if (paramElement is not T typedElement)
    {
      value = default;
      return false;
    }

    value = typedElement;
    return true;
  }

  public T GetValueAsDocumentObject<T>(IRevitElement element, RevitBuiltInParameter builtInParameter)
    where T : class
  {
    if (!TryGetValueAsDocumentObject<T>(element, builtInParameter, out var value))
    {
      throw new SpeckleConversionException($"Failed to get {builtInParameter} as an element of type {typeof(T)}");
    }

    return value; // If TryGet returns true, we succeeded in obtaining the value, and it will not be null.
  }

  private TResult? GetValueGeneric<TResult>(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    IRevitStorageType expectedStorageType,
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
    var parameter = element.GetParameter(builtInParameter).NotNull();
    return GetValueGeneric(parameter, expectedStorageType, getParamValue);
  }

  private TResult? GetValueGeneric<TResult>(
    IRevitParameter parameter,
    IRevitStorageType expectedStorageType,
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

      if (param.GetBuiltInParameter() is RevitBuiltInParameter bip && (usedParameters?.Contains(bip) ?? false))
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
