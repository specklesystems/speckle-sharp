using System.Diagnostics.CodeAnalysis;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Helpers;

//not auto because of NotNullWhen
public interface IParameterValueExtractor
{
  object? GetValue(IRevitParameter parameter);
  double GetValueAsDouble(IRevitElement element, RevitBuiltInParameter builtInParameter);
  bool TryGetValueAsDouble(IRevitElement element, RevitBuiltInParameter builtInParameter, out double? value);
  int GetValueAsInt(IRevitElement element, RevitBuiltInParameter builtInParameter);
  bool? GetValueAsBool(IRevitElement element, RevitBuiltInParameter builtInParameter);
  string? GetValueAsString(IRevitElement element, RevitBuiltInParameter builtInParameter);
  IRevitElementId GetValueAsElementId(IRevitElement element, RevitBuiltInParameter builtInParameter);
  bool TryGetValueAsElementId(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    out IRevitElementId? elementId
  );
  IRevitElementId? GetValueAsElementId(IRevitParameter parameter);
  IRevitLevel GetValueAsRevitLevel(IRevitElement element, RevitBuiltInParameter builtInParameter);
  bool TryGetValueAsRevitLevel(
    IRevitElement element,
    RevitBuiltInParameter builtInParameter,
    [NotNullWhen(true)] out IRevitLevel? revitLevel
  );
  Dictionary<string, IRevitParameter> GetAllRemainingParams(IRevitElement revitElement);
  void RemoveUniqueId(string uniqueId);
}
