using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.Helpers;

public class ParameterValueExtractor
{
  private readonly ToSpeckleScalingService _scalingService;
  private readonly HashSet<BuiltInParameter> _usedParameters = new();

  public ParameterValueExtractor(ToSpeckleScalingService scalingService)
  {
    _scalingService = scalingService;
  }

  public object? GetValue(Parameter parameter)
  {
    return parameter.StorageType switch
    {
      StorageType.Double => GetValueAsDouble(parameter),
      StorageType.Integer => GetValueAsInt(parameter),
      StorageType.String => GetValueAsString(parameter),
      StorageType.ElementId => GetValueAsElementId(parameter)?.ToString(),
      _ => throw new SpeckleConversionException($"Unsupported parameter storage type {parameter.StorageType}")
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

  private TResult? GetValueGeneric<TResult>(
    Element element,
    BuiltInParameter builtInParameter,
    StorageType expectedStorageType,
    Func<DB.Parameter, TResult> getParamValue
  )
  {
    _usedParameters.Add(builtInParameter);
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
}
