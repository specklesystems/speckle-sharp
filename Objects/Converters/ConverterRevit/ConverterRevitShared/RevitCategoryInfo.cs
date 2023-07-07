using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using OSG = Objects.Structural.Geometry;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;
using RevitSharedResources.Helpers;
using SHC = RevitSharedResources.Helpers.Categories;

namespace Objects.Converter.Revit
{
  /// <summary>
  /// Container of all pre-defined categories in Revit that incoming Base objects can be grouped under
  /// </summary>
  public class AllRevitCategories : IAllRevitCategories
  {
    #region IAllRevitCategories
    public IRevitCategoryInfo UndefinedCategory => SHC.Undefined;
    public IEnumerable<IRevitCategoryInfo> All => SHC.All.Values;
    public IRevitCategoryInfo GetRevitCategoryInfo(Base @base)
    {
      return GetRevitCategoryInfoStatic(@base);
    }
    public IRevitCategoryInfo GetRevitCategoryInfo(string categoryName)
    {
      return GetRevitCategoryInfoStatic(categoryName);
    }
    #endregion

    public static RevitCategoryInfo GetRevitCategoryInfoStatic(Base @base)
    {
      var categoryInfo = @base switch
      {
        BER.AdaptiveComponent _ => SHC.FamilyInstance,
        BE.Beam _ => SHC.StructuralFraming,
        BE.Brace _ => SHC.StructuralFraming,
        BE.Column _ => SHC.Column,
#if !REVIT2020 && !REVIT2021
        BE.Ceiling _ => SHC.Ceiling,
#endif
        BER.FamilyInstance _ => SHC.FamilyInstance,
        BE.Floor _ => SHC.Floor,
        BE.Roof _ => SHC.Roof,
        BE.Wall _ => SHC.Wall,
        BE.Duct _ => SHC.Duct,
        BE.Pipe _ => SHC.Pipe,
        BE.Wire _ => SHC.Wire,
        BE.CableTray _ => SHC.CableTray,
        BE.Conduit _ => SHC.Conduit,
        BE.Revit.RevitRailing _ => SHC.Railing,
        Other.Revit.RevitInstance _ => SHC.FamilyInstance,
        OSG.Element1D e when e.type == OSG.ElementType1D.Beam => SHC.StructuralFraming,
        OSG.Element1D e when e.type == OSG.ElementType1D.Brace => SHC.StructuralFraming,
        OSG.Element1D e when e.type == OSG.ElementType1D.Column => SHC.Column,
        OSG.Element2D => SHC.Floor,
        _ => SHC.Undefined,
      };

      if (categoryInfo != SHC.FamilyInstance) return categoryInfo;

      var instanceCategory = @base["category"] as string;
      if (string.IsNullOrEmpty(instanceCategory)) return categoryInfo;

      var newCategoryInfo = GetRevitCategoryInfoStatic(instanceCategory);

      if (newCategoryInfo != SHC.Undefined) return newCategoryInfo;
      return categoryInfo;
    }

    /// <summary>
    /// Get's the <see cref="RevitCategoryInfo"/> from the category name. This is the least performant method to get the <see cref="RevitCategoryInfo"/>, so prefer <see cref="GetRevitCategoryInfo{T}(Base)"/> or <see cref="GetRevitCategoryInfoOfSpeckleObject(Base)"/>
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public static RevitCategoryInfo GetRevitCategoryInfoStatic(string categoryName)
    {
      categoryName = categoryName.Replace(" ", "");
      if (SHC.All.TryGetValue(categoryName, out var match))
      {
        return match;
      }

      foreach (var categoryInfo in SHC.All.Values)
      {
        if (categoryName.IndexOf(categoryInfo.CategoryName, StringComparison.OrdinalIgnoreCase) >= 0) 
        {
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

      return SHC.Undefined;
    }
    public IRevitCategoryInfo GetRevitCategoryInfo<T>(Base @base)
    {
      return GetRevitCategoryInfoStatic<T>(@base);
    }
    public static RevitCategoryInfo GetRevitCategoryInfoStatic<T>(Base @base)
    {
      var elementType = GetRevitCategoryInfoStatic(@base);
      if (elementType != SHC.Undefined) return elementType;

      var matchingTypes = SHC.All
        .Where(kvp => kvp.Value.ElementTypeType == typeof(T))
        .Select(kvp => kvp.Value);

      foreach (var matchingType in matchingTypes)
      {
        if (matchingType.BuiltInCategories.Count == 0)
        {
          return matchingType;
        }
      }

      var newCatInfo = new RevitCategoryInfo(typeof(T).Name, null, typeof(T), new List<BuiltInCategory>());
      SHC.All[typeof(T).Name] = newCatInfo;
      return newCatInfo;
    }
  }
}
