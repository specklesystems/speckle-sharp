#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using SCH = RevitSharedResources.Helpers.Categories;

namespace RevitSharedResources.Extensions.SpeckleExtensions
{
  public static class IRevitDocumentAggregateCacheExtensions
  {
    public static IRevitObjectCache<T> GetOrInitializeWithDefaultFactory<T>(this IRevitDocumentAggregateCache cache)
    {
      MethodInfo cacheFactoryMethod = null;
      foreach (var method in typeof(IRevitDocumentAggregateCacheExtensions).GetMethods())
      {
        var firstParam = method.GetParameters()[0];
        if (firstParam.GetType() != typeof(IRevitObjectCache<T>)) continue;

        cacheFactoryMethod = method;
        break;
      }

      if (cacheFactoryMethod == null)
      {
        throw new ArgumentException($"Cannot use {nameof(GetOrInitializeWithDefaultFactory)} with the generic parameter {typeof(T).Name} because there is no default factory defined for that object");
      }

      return cache.GetOrInitializeCacheOfType<T>(singleCache =>
      {
        cacheFactoryMethod.Invoke(null, new object[] { singleCache, cache.Document });
      }, out _);
    }

    public static void CacheInitializer(IRevitObjectCache<Category> cache, Document doc)
    {
      var _categories = new Dictionary<string, Category>();
      foreach (var bic in SCH.SupportedBuiltInCategories)
      {
        var category = Category.GetCategory(doc, bic);
        if (category == null)
          continue;
        //some categories, in other languages (eg DEU) have duplicated names #542
        if (_categories.ContainsKey(category.Name))
        {
          var spec = category.Id.ToString();
          if (category.Parent != null)
            spec = category.Parent.Name;
          _categories.Add($"{category.Name} ({spec})", category);
        }
        else
          _categories.Add(category.Name, category);
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
      cache.AddMany(predefinedCategories, categoryInfo => nameof(categoryInfo));
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
}
