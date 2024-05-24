using Autodesk.Revit.DB;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Revit2023.Api;

public class RevitUnitUtils : IRevitUnitUtils
{
  public double ConvertFromInternalUnits(double value, IRevitForgeTypeId forgeTypeId) =>
    UnitUtils.ConvertFromInternalUnits(value, ((IRevitForgeTypeIdProxy)forgeTypeId)._Instance);
}

public static class RevitSpecTypeId
{
  public static IRevitForgeTypeId Length => new ForgeTypeIdProxy(SpecTypeId.Length);
}
