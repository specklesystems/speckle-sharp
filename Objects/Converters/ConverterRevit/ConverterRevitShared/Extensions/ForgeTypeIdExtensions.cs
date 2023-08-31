#nullable enable
#if !REVIT2020
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitShared.Extensions
{
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
    private readonly static HashSet<string> lengthTypes = new()
    {
      UnitTypeId.Millimeters.ToUniqueString(),
      UnitTypeId.Centimeters.ToUniqueString(),
      UnitTypeId.Meters.ToUniqueString(),
      UnitTypeId.Inches.ToUniqueString(),
      UnitTypeId.Feet.ToUniqueString(),
      UnitTypeId.FeetFractionalInches.ToUniqueString(),
    };
    public static bool IsLengthType(this ForgeTypeId forgeTypeId)
    {
      if (lengthTypes.Contains(forgeTypeId.ToUniqueString()))
      {
        return true;
      }
      return false;
    }
  }
}
#endif
