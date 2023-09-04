#nullable enable
#if REVIT2020
using System.Linq;
using Autodesk.Revit.DB;

namespace ConverterRevitShared.Extensions
{
  internal static class DisplayUnitTypeExtensions
  {
    public static string? GetSymbol(this DisplayUnitType displayUnitType)
    {
      var validSymbols = FormatOptions.GetValidUnitSymbols(displayUnitType);
      var unitSymbolTypes = validSymbols.Where(x => x != UnitSymbolType.UST_NONE);
      foreach (var symbolId in unitSymbolTypes)
      {
        return LabelUtils.GetLabelFor(symbolId);
      }
      return null;
    }

    public static string ToUniqueString(this DisplayUnitType displayUnitType)
    {
      return displayUnitType.ToString();
    }
  }
}
#endif
