using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;

namespace RevitSharedResources.Extensions.SpeckleExtensions;

public static class IRevitObjectCacheExtensions
{
  public static List<ElementType> GetOrAddGroupOfTypes(
    this IRevitObjectCache<List<ElementType>> cache,
    IRevitCategoryInfo categoryInfo
  )
  {
    var elementTypes = cache.GetOrAdd(
      categoryInfo.CategoryName,
      () => categoryInfo.GetElementTypes(cache.ParentCache.Document),
      out bool isExistingValue
    );

    // if type was added instead of retreived, add types to master cache to facilitate lookup later
    if (!isExistingValue)
    {
      cache.ParentCache
        .GetOrInitializeWithDefaultFactory<ElementType>()
        .AddMany(elementTypes, type => categoryInfo.GetCategorySpecificTypeName(type.Name));
    }
    return elementTypes;
  }
}
