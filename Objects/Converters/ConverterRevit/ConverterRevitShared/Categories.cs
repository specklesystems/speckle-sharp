using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Objects.Converter.Revit
{
  public static class Categories
  {
    public static readonly List<BuiltInCategory> columnCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns };
    public static readonly List<BuiltInCategory> beamCategories = new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming };
    public static readonly List<BuiltInCategory> ductCategories = new List<BuiltInCategory> { BuiltInCategory.OST_DuctCurves };
    public static readonly List<BuiltInCategory> wallCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Walls };
    public static readonly List<BuiltInCategory> floorCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Floors };
    public static readonly List<BuiltInCategory> curtainWallSubElements = new List<BuiltInCategory> { BuiltInCategory.OST_CurtainWallMullions, BuiltInCategory.OST_CurtainWallPanels };

    public static bool Contains(this IEnumerable<BuiltInCategory> categories, Category category)
    {
      try
      {
        return categories.Select(x => (int)x).Contains(category.Id.IntegerValue);
      }
      catch
      {
        return false;
      }

    }

    public static RevitCategory GetCategory(string builtInCategory)
    {
      return (RevitCategory)BuiltInCategoryNames.IndexOf(builtInCategory);
    }

    public static string GetBuiltInCategory(RevitCategory c)
    {
      return BuiltInCategoryNames[(int)c];
    }

    internal static List<string> BuiltInCategoryNames = new List<string>
    {
      "OST_Casework",
      "OST_Ceilings",
      "OST_Columns",
      "OST_CurtainWallPanels",
      "OST_CurtaSystem",
      "OST_CurtainWallMullions",
      "OST_Doors",
      "OST_Entourage",
      "OST_Fascia",
      "OST_Floors",
      "OST_Furniture",
      "OST_FurnitureSystems",
      "OST_GenericModel",
      "OST_Gutter",
      "OST_StairsLandings",
      "OST_Mass",
      "OST_StairsRailing",
      "OST_Planting",
      "OST_Ramps",
      "OST_Roads",
      "OST_RoofSoffit",
      "OST_Roofs",      
      "OST_StairsRuns",
      "OST_Site",
      "OST_SpecialityEquipment",
      "OST_Stairs",
      "OST_AreaRein",
      "OST_StructuralFramingSystem",
      "OST_StructuralColumns",
      "OST_StructConnections",
      "OST_StructuralFoundation",
      "OST_StructuralFraming",
      "OST_Rebar",
      "OST_StructuralStiffener",
      "OST_StructuralTruss",
      "OST_StairsSupports",
      "OST_Topography",
      "OST_Walls",
      "OST_Windows"
    };


  }
}