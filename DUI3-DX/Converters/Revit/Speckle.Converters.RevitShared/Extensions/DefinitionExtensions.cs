using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Extensions;

public static class DefinitionExtensions
{
  // POC: can we just interface these specialisations out and thereby avoid this kind of BS :D
  public static string GetUnitTypeString(this IRevitDefinition definition)
  {
    return definition.GetDataType().TypeId;
  }
}
