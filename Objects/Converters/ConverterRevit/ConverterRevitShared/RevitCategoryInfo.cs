using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using OSG = Objects.Structural.Geometry;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit
{
  public struct RevitCategoryInfo : IRevitCategoryInfo<BuiltInCategory>
  {
    public RevitCategoryInfo(string name, Type instanceType, Type familyType, List<BuiltInCategory> categories, List<string> categoryAliases = null)
    {
      CategoryName = name;
      ElementInstanceType = instanceType;
      ElementTypeType = familyType;
      BuiltInCategories = categories;
      CategoryAliases = categoryAliases ?? new List<string>();
    }
    public string CategoryName { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public List<BuiltInCategory> BuiltInCategories { get; }
    public List<string> CategoryAliases { get; }

    public bool ContainsRevitCategory(Category category)
    {
      return BuiltInCategories.Select(x => (int)x).Contains(category.Id.IntegerValue);
    }
    public static bool operator ==(RevitCategoryInfo left, RevitCategoryInfo right) => left.Equals(right);
    public static bool operator !=(RevitCategoryInfo left, RevitCategoryInfo right) => !(left == right);
  }
  /// <summary>
  /// Container of all pre-defined categories in Revit that incoming Base objects can be grouped under
  /// </summary>
  public class AllRevitCategories : IAllRevitCategories<BuiltInCategory>
  {
    public static Dictionary<string, RevitCategoryInfo> All { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
      { nameof(CableTray), CableTray },
      { nameof(Ceiling), Ceiling },
      { nameof(Column), Column },
      { nameof(Conduit), Conduit },
      { nameof(Duct), Duct },
      { nameof(Floor), Floor },
      { nameof(FamilyInstance), FamilyInstance },
      { nameof(Pipe), Pipe },
      { nameof(Roof), Roof },
      { nameof(Railing), Railing },
      { nameof(StructuralFraming), StructuralFraming },
      { nameof(Wall), Wall },
      { nameof(Wire), Wire },
      { nameof(Undefined), Undefined },
    };
    public static RevitCategoryInfo CableTray { get; } = new(
      nameof(CableTray),
      typeof(DB.Electrical.CableTray),
      typeof(DB.Electrical.CableTrayType),
      new List<BuiltInCategory>()
    );
    
    public static RevitCategoryInfo Ceiling { get; } = new(
      nameof(Ceiling),
      typeof(DB.Ceiling),
      typeof(CeilingType),
      new List<BuiltInCategory>()
    );
    public static RevitCategoryInfo Column { get; } = new(
      nameof(Column),
      typeof(FamilyInstance),
      typeof(FamilySymbol),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_StructuralColumns
      });
    public static RevitCategoryInfo Conduit { get; } = new(
      nameof(Conduit),
      typeof(DB.Electrical.Conduit),
      typeof(DB.Electrical.ConduitType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_StructuralColumns
      });
    public static RevitCategoryInfo Duct { get; } = new(
      nameof(Duct),
      typeof(DB.Mechanical.Duct),
      typeof(DB.Mechanical.FlexDuctType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_FlexDuctCurves
      });
    public static RevitCategoryInfo Floor { get; } = new(
      nameof(Floor),
      typeof(DB.Floor),
      typeof(DB.FloorType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Floors
      });
    
    public static RevitCategoryInfo FamilyInstance { get; } = new(
      nameof(FamilyInstance),
      typeof(DB.FamilyInstance),
      typeof(DB.FamilySymbol),
      new List<BuiltInCategory>()
      );

    //public static RevitCategoryInfo Material { get; } = new(
    //  nameof(Material), 
    //  typeof(DB.Material), 
    //  null,
    //  new List<BuiltInCategory> 
    //  { 
    //    BuiltInCategory.OST_Materials, 
    //    BuiltInCategory.OST_PipeMaterials,
    //    BuiltInCategory.OST_WireMaterials
    //  });

    public static RevitCategoryInfo Pipe { get; } = new(
      nameof(Pipe),
      typeof(DB.Plumbing.Pipe),
      typeof(DB.Plumbing.FlexPipeType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_FlexPipeCurves
      });

    public static RevitCategoryInfo Roof { get; } = new(
      nameof(Roof),
      typeof(DB.RoofBase),
      typeof(DB.RoofType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Roofs,
      });
    public static RevitCategoryInfo Railing { get; } = new(
      nameof(Railing),
      typeof(DB.Architecture.Railing),
      typeof(DB.Architecture.RailingType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Roofs,
      });

    public static RevitCategoryInfo StructuralFraming { get; } = new(
      nameof(StructuralFraming),
      typeof(DB.FamilyInstance),
      typeof(DB.FamilySymbol),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_StructuralFraming
      },
      new List<string> { "beam", "brace", "framing" });

    public static RevitCategoryInfo Wall { get; } = new(
      nameof(Wall),
      typeof(DB.Wall),
      typeof(DB.WallType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Walls
      });
    
    public static RevitCategoryInfo Wire { get; } = new(
      nameof(Wire),
      typeof(DB.Electrical.Wire),
      typeof(DB.Electrical.WireType),
      new List<BuiltInCategory>()
      );

    public static RevitCategoryInfo Undefined { get; } = new (
      nameof(Undefined),
      null,
      null,
      new List<BuiltInCategory>()
      );

    #region IAllRevitCategories
    public IRevitCategoryInfo<BuiltInCategory> UndefinedCategory => Undefined;

    public IRevitCategoryInfo<BuiltInCategory> GetRevitCategoryInfo(Base @base)
    {
      return GetRevitCategoryInfoStatic(@base);
    }
    public IRevitCategoryInfo<BuiltInCategory> GetRevitCategoryInfo(string categoryName)
    {
      return GetRevitCategoryInfoStatic(categoryName);
    }
    #endregion

    public static RevitCategoryInfo GetRevitCategoryInfoStatic(Base @base)
    {
      return @base switch
      {
        BER.AdaptiveComponent _ => FamilyInstance,
        BE.Beam _ => StructuralFraming,
        BE.Brace _ => StructuralFraming,
        BE.Column _ => Column,
#if !REVIT2020 && !REVIT2021
        BE.Ceiling _ => Ceiling,
#endif
        BER.FamilyInstance _ => FamilyInstance,
        BE.Floor _ => Floor,
        BE.Roof _ => Roof,
        BE.Wall _ => Wall,
        BE.Duct _ => Duct,
        BE.Pipe _ => Pipe,
        BE.Wire _ => Wire,
        BE.CableTray _ => CableTray,
        BE.Conduit _ => Conduit,
        BE.Revit.RevitRailing _ => Railing,
        Other.Revit.RevitInstance _ => FamilyInstance,
        OSG.Element1D e when e.type == OSG.ElementType1D.Beam => StructuralFraming,
        OSG.Element1D e when e.type == OSG.ElementType1D.Brace => StructuralFraming,
        OSG.Element1D e when e.type == OSG.ElementType1D.Column => Column,
        OSG.Element2D => Floor,
        _ => Undefined,
      };
    }

    /// <summary>
    /// Get's the <see cref="RevitCategoryInfo"/> from the category name. This is the least performant method to get the <see cref="RevitCategoryInfo"/>, so prefer <see cref="GetRevitCategoryInfo{T}(Base)"/> or <see cref="GetRevitCategoryInfoOfSpeckleObject(Base)"/>
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public static RevitCategoryInfo GetRevitCategoryInfoStatic(string categoryName)
    {
      var match = All[categoryName];
      if (match != default) return match;

      foreach (var categoryInfo in All.Values)
      {
        if (categoryName.IndexOf(categoryInfo.CategoryName, StringComparison.OrdinalIgnoreCase) >= 0) {
          return categoryInfo;
        }
        foreach (var alias in categoryInfo.CategoryAliases)
        {
          if (categoryName.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0)
          {
            return categoryInfo;
          }
        }
      }

      return Undefined;
    }
    public IRevitCategoryInfo<BuiltInCategory> GetRevitCategoryInfo<T>(Base @base)
    {
      return GetRevitCategoryInfoStatic<T>(@base);
    }
    public static RevitCategoryInfo GetRevitCategoryInfoStatic<T>(Base @base)
    {
      var elementType = GetRevitCategoryInfoStatic(@base);
      if (elementType != Undefined) return elementType;

      var matchingTypes = All
        .Where(kvp => kvp.Value.ElementTypeType == typeof(T))
        .Select(kvp => kvp.Value);

      foreach (var matchingType in matchingTypes)
      {
        if (matchingType.BuiltInCategories.Count == 0)
        {
          return matchingType;
        }
      }
      return new RevitCategoryInfo(typeof(T).Name, null, typeof(T), new List<BuiltInCategory>());
    }
  }
}
