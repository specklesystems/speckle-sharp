#nullable enable
#if !REVIT2020
using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitShared.Extensions;

public static class ForgeTypeIdExtensions
{
  public static string? GetSymbol(this ForgeTypeId forgeTypeId)
  {
    if (!FormatOptions.CanHaveSymbol(forgeTypeId))
    {
      return null;
    }
    var validSymbols = FormatOptions.GetValidSymbols(forgeTypeId);
    var typeId = validSymbols?.Where(x => !x.Empty());
    foreach (DB.ForgeTypeId symbolId in typeId)
    {
      return LabelUtils.GetLabelForSymbol(symbolId);
    }
    return null;
  }

  public static string ToUniqueString(this ForgeTypeId forgeTypeId)
  {
    return forgeTypeId.TypeId;
  }
}
#endif
