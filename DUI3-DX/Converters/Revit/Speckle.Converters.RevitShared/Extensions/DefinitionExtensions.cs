using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.Extensions;

public static class DefinitionExtensions
{
  public static string GetUnitTypeString(this Definition definition)
  {
#if REVIT2020 || REVIT2021
    return definition.UnitType.ToString();
#else
    return definition.GetDataType().TypeId;
#endif
  }
}
