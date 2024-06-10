using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Extensions;

public static class ForgeTypeIdExtensions
{
  public static string? GetSymbol(this IRevitForgeTypeId forgeTypeId, IRevitFormatOptionsUtils formatOptionsUtils)
  {
    if (!formatOptionsUtils.CanHaveSymbol(forgeTypeId))
    {
      return null;
    }
    var validSymbols = formatOptionsUtils.GetValidSymbols(forgeTypeId);
    var typeId = validSymbols.Where(x => !x.Empty());
    foreach (var symbolId in typeId)
    {
      return formatOptionsUtils.GetLabelForSymbol(symbolId);
    }
    return null;
  }

  public static string ToUniqueString(this IRevitForgeTypeId forgeTypeId)
  {
    return forgeTypeId.TypeId;
  }
}
