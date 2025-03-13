#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using SCH = RevitSharedResources.Helpers.Categories;

namespace RevitSharedResources.Extensions.SpeckleExtensions;

public static class IRevitDocumentAggregateCacheExtensions
{
  public static IRevitObjectCache<T> GetOrInitializeWithDefaultFactory<T>(this IRevitDocumentAggregateCache cache)
  {
    return cache.GetOrInitializeCacheOfType<T>(
      singleCache =>
      {
        MethodInfo cacheFactoryMethod = null;
        foreach (var method in typeof(IRevitDocumentAggregateCacheExtensions).GetMethods())
        {
          var firstParam = method.GetParameters().FirstOrDefault();
          if (firstParam == null || firstParam.ParameterType != typeof(IRevitObjectCache<T>))
          {
            continue;
          }

          cacheFactoryMethod = method;
          break;
        }

        if (cacheFactoryMethod == null)
        {
          throw new ArgumentException(
            $"Cannot use {nameof(GetOrInitializeWithDefaultFactory)} with the generic parameter {typeof(T).Name} because there is no default factory defined for that object"
          );
        }

        cacheFactoryMethod.Invoke(null, new object[] { singleCache, cache.Document });
      },
      out _
    );
  }

  public static void CacheInitializer(IRevitObjectCache<Category> cache, Document doc)
  {
    var _categories = new Dictionary<string, Category>();

    // Document.Settings.Categories only returns the parent categories.
    // To avoid iterating over all subcategories of each parent category, we add some extra categories that are not returned by Document.Settings.Categories #3615
    var extraCategories = new Category[]
    {
      Category.GetCategory(doc, BuiltInCategory.OST_Gutter),
      Category.GetCategory(doc, BuiltInCategory.OST_Fascia),
      Category.GetCategory(doc, BuiltInCategory.OST_RoofSoffit),
      Category.GetCategory(doc, BuiltInCategory.OST_EdgeSlab), // Slab Edges
      Category.GetCategory(doc, BuiltInCategory.OST_Cornices), // Wall Sweeps
    };
    var allCategories = doc.Settings.Categories.Cast<Category>().Concat(extraCategories);

    foreach (Category category in allCategories)
    {
      if (!Helpers.Extensions.Extensions.IsCategorySupported(category))
      {
        continue;
      }

      //some categories, in other languages (eg DEU) have duplicated names #542
      if (_categories.ContainsKey(category.Name))
      {
        var spec = category.Id.ToString();
        if (category.Parent != null)
        {
          spec = category.Parent.Name;
        }

        _categories.Add($"{category.Name} ({spec})", category);
      }
      else
      {
        _categories.Add(category.Name, category);
      }
    }

    cache.AddMany(_categories);
  }

  public static void CacheInitializer(IRevitObjectCache<IRevitCategoryInfo> cache, Document doc)
  {
    var predefinedCategories = new List<IRevitCategoryInfo>();
    foreach (var property in typeof(SCH).GetProperties(BindingFlags.Static | BindingFlags.Public))
    {
      if (property.GetValue(null) is IRevitCategoryInfo categoryInfo)
      {
        predefinedCategories.Add(categoryInfo);
      }
    }
    cache.AddMany(predefinedCategories, categoryInfo => categoryInfo.CategoryName);
  }

  public static void CacheInitializer(IRevitObjectCache<ElementType> cache, Document doc)
  {
    // don't do any default initialization
  }

  public static void CacheInitializer(IRevitObjectCache<List<ElementType>> cache, Document doc)
  {
    // don't do any default initialization
  }
}
