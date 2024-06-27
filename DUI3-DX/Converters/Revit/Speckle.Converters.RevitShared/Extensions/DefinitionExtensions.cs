using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.Extensions;

public static class DefinitionExtensions
{
  // POC: can we just interface these specialisations out and thereby avoid this kind of BS :D
  public static string GetUnitTypeString(this Definition definition)
  {
#if REVIT2020 || REVIT2021
    return definition.UnitType.ToString();
#else
    return definition.GetDataType().TypeId;
#endif
  }
}
