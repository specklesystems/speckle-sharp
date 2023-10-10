#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Extensions.SpeckleExtensions;
using RevitSharedResources.Helpers;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;
using OSG = Objects.Structural.Geometry;
using SHC = RevitSharedResources.Helpers.Categories;

namespace Objects.Converter.Revit
{
  /// <summary>
  /// Container of all pre-defined categories in Revit that incoming Base objects can be grouped under
  /// </summary>
  public class AllRevitCategories : IAllRevitCategories
  {
    private IRevitDocumentAggregateCache revitDocumentAggregateCache;

    public AllRevitCategories(IRevitDocumentAggregateCache revitDocumentAggregateCache)
    {
      this.revitDocumentAggregateCache = revitDocumentAggregateCache;
    }

    #region IAllRevitCategories

    public IRevitCategoryInfo GetRevitCategoryInfo<T>(Base @base)
    {
      var elementType = GetRevitCategoryInfo(@base);
      if (elementType != SHC.Undefined) return elementType;

      var matchingType = revitDocumentAggregateCache
        .GetOrInitializeWithDefaultFactory<IRevitCategoryInfo>()
        .GetAllObjects()
        .Where(catInfo => catInfo.ElementTypeType == typeof(T)
          && catInfo.BuiltInCategories.Count == 0)
        .FirstOrDefault();

      if (matchingType != null)
      {
        return matchingType;
      }

      var categoryInfo = revitDocumentAggregateCache
        .GetOrInitializeWithDefaultFactory<IRevitCategoryInfo>()
        .GetOrAdd(typeof(T).Name, () =>
        {
          return new RevitCategoryInfo(typeof(T).Name, null, typeof(T), new List<BuiltInCategory>());
        }, out _);

      return categoryInfo;
    }

    public IRevitCategoryInfo GetRevitCategoryInfo(Base @base)
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

      // family instance and undefined categories are very broad, so we can try to narrow those down
      if (categoryInfo != SHC.FamilyInstance && categoryInfo != SHC.Undefined)
      {
        return categoryInfo;
      }

      //2.16 onwards we check for "builtInCategory"
      var instanceCategory = @base["builtInCategory"] as string;
      //pre 2.16 we used the inconsistent, display value "category"
      if (string.IsNullOrEmpty(instanceCategory))
        instanceCategory = @base["category"] as string;

      if (string.IsNullOrEmpty(instanceCategory))
        return categoryInfo;

      var newCategoryInfo = GetRevitCategoryInfo(instanceCategory);

      if (newCategoryInfo != SHC.Undefined) return newCategoryInfo;
      return categoryInfo;
    }
    public IRevitCategoryInfo GetRevitCategoryInfo(string categoryName)
    {
      var categoryInfo = GetCategoryInfoForObjectWithExactName(categoryName);
      if (categoryInfo != null) return categoryInfo;


      categoryName = CategoryNameFormatted(categoryName);
      var revitCategoryInfoCache = revitDocumentAggregateCache
        .GetOrInitializeWithDefaultFactory<IRevitCategoryInfo>();

      categoryInfo = revitCategoryInfoCache
        .TryGet(categoryName);
      if (categoryInfo != null) return categoryInfo;


      foreach (var info in revitCategoryInfoCache.GetAllObjects())
      {
        if (categoryName.IndexOf(info.CategoryName, StringComparison.OrdinalIgnoreCase) >= 0)
        {
          return info;
        }
        foreach (var alias in info.CategoryAliases)
        {
          if (categoryName.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0)
          {
            return info;
          }
        }
      }

      return SHC.Undefined;
    }
    #endregion



    private IRevitCategoryInfo? GetCategoryInfoForObjectWithExactName(string unformattedCatName)
    {
      var bic = BuiltInCategory.INVALID;
      string formattedName = "";
      // 2.16 onwards we're passing the "builtInCategory" string
      if (unformattedCatName.StartsWith("OST"))
      {
        if (!Enum.TryParse(unformattedCatName, out bic))
        {
          return null;
        }
        formattedName = unformattedCatName.Replace("OST_", "");
      }
      // pre 2.16 we're passing the "category" string
      else
      {
        var revitCat = revitDocumentAggregateCache
          .GetOrInitializeWithDefaultFactory<Category>()
          .TryGet(unformattedCatName);

        if (revitCat == null) return null;

        bic = Categories.GetBuiltInCategory(revitCat);
        formattedName = CategoryNameFormatted(unformattedCatName);
      }

      return revitDocumentAggregateCache
        .GetOrInitializeWithDefaultFactory<IRevitCategoryInfo>()
        .GetOrAdd(formattedName, () =>
        {
          return new RevitCategoryInfo(formattedName, null, null, new List<BuiltInCategory> { bic });
        }, out _);
    }

    private static string CategoryNameFormatted(string name)
    {
      return name.Replace(" ", "");
    }
  }
}
