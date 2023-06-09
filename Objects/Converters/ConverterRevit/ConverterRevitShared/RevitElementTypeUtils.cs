#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Classes;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using OSG = Objects.Structural.Geometry;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  /// <summary>
  /// Functionality related to retrieving, setting, and caching <see cref="DB.ElementType"/>s. Implements <see cref="IRevitElementTypeRetriever{TElementType, TBuiltInCategory}"/> and <see cref="IAllRevitCategoriesExposer{TBuiltInCategory}"/> in order for the connector to be able to access object info through the converter during mapping on receive.
  /// </summary>
  public partial class ConverterRevit : IRevitElementTypeRetriever<ElementType, BuiltInCategory>,
    IAllRevitCategoriesExposer<BuiltInCategory>
  {
    private ConversionOperationCache ConversionOperationCache { get; } = new();
    private static AllRevitCategories AllRevitCategoriesInstance { get; } = new();
    public IAllRevitCategories<BuiltInCategory> AllCategories => AllRevitCategoriesInstance;

    #region IRevitElementTypeRetriever
    public string? GetElementType(Base @base)
    {
      string type = null;
      switch (@base)
      {
        case OSG.Element1D el:
          type = el.property?.name;
          break;
        case OSG.Element2D el:
          type = el.property?.name;
          break;
        case Other.Revit.RevitInstance el:
          type = el.typedDefinition?.type;
          break;
        case BuiltElements.TeklaStructures.TeklaBeam el:
          type = el.profile?.name;
          break;
      };
      return type ?? @base["type"] as string;
    }
    
    public void SetElementType(Base @base, string type)
    {
      switch (@base)
      {
        case OSG.Element1D el:
          if (el.property == null) goto default;
          el.property.name = type;
          break;
        case OSG.Element2D el:
          if (el.property == null) goto default;
          el.property.name = type;
          break;
        case Other.Revit.RevitInstance el:
          if (el.typedDefinition == null) goto default;
          el.typedDefinition.type = type;
          break;
        case BuiltElements.TeklaStructures.TeklaBeam el:
          if (el.profile == null) goto default;
          el.profile.name = type;
          break;
        default:
          @base["type"] = type;
          break;
      };
    }

    public bool CacheContainsTypeWithName(string category, string baseType)
    {
      var type = ConversionOperationCache.TryGet<ElementType>(GetUniqueTypeName(category, baseType));
      if (type == null) return false;

      return true;
    }

    public IEnumerable<ElementType> GetAllCachedElementTypes()
    {
      return ConversionOperationCache.GetAllObjectsOfType<ElementType>();
    }
    
    public IEnumerable<ElementType> GetOrAddAvailibleTypes(IRevitCategoryInfo<BuiltInCategory> typeInfo)
    {
      var types = ConversionOperationCache.GetOrAdd<IEnumerable<ElementType>>(
        typeInfo.CategoryName,
        () => GetElementTypes<ElementType>(typeInfo.ElementTypeType, typeInfo.BuiltInCategories),
        out var typesRetrieved);

      // if type was added instead of retreived, add types to master cache to facilitate lookup later
      if (!typesRetrieved)
      {
        ConversionOperationCache.AddMany<ElementType>(types, type => GetUniqueTypeName(typeInfo.CategoryName, type.Name));
      }

      return types;
    }

    public void InvalidateElementTypeCache(string categoryName)
    {
      ConversionOperationCache.Invalidate<IEnumerable<ElementType>>(categoryName);
    }

    #endregion

    private string GetUniqueTypeName(string category, string type)
    {
      return category + "_" + type;
    }

    private T? GetElementType<T>(Base element, ApplicationObject appObj, out bool isExactMatch)
      where T : ElementType
    {
      var type = GetElementType(element);
      if (type == null)
      {
        throw new ArgumentException($"Could not find valid type of element of type \"{element.speckle_type}\"");
      }
      var typeInfo = Revit.AllRevitCategories.GetRevitCategoryInfoStatic<T>(element);
      var types = GetOrAddAvailibleTypes(typeInfo);

      isExactMatch = false;
      if (!types.Any())
      {
        var name = typeof(T).Name;
        if (element["category"] is string category && !string.IsNullOrWhiteSpace(category))
          name = category;

        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not find any loaded family to use for category {name}.");

        return default;
      }

      var exactType = ConversionOperationCache.TryGet<ElementType>(GetUniqueTypeName(typeInfo.CategoryName, type));

      if (exactType != null)
      {
        isExactMatch = true;
        if (exactType is FamilySymbol fs && !fs.IsActive)
          fs.Activate();
        return (T)exactType;
      }

      var family = (element["family"] as string)?.ToLower();

      return GetElementType<T>(element, family, type, types, appObj, out isExactMatch);
    }

    private T GetElementType<T>(Base element, string? family, string type, IEnumerable<ElementType> types, ApplicationObject appObj, out bool isExactMatch)
    {
      isExactMatch = false;
      ElementType match = null;

      if (!string.IsNullOrEmpty(family)) // try and match the family only.
      {
        match = types.FirstOrDefault(x => x.FamilyName?.ToLower() == family);
      }
      
      if (match == null)
      {
        if (element is Objects.BuiltElements.Wall) // specifies the basic wall sub type as default
          match = types.Cast<WallType>().Where(o => o.Kind == WallKind.Basic).Cast<ElementType>().FirstOrDefault();
        match ??= types.First();
      }

      if (!isExactMatch)
        appObj.Update(logItem: $"Missing type. Family: {family} Type: {type}\nType was replaced with: {match.FamilyName}, {match.Name}");

      if (match is FamilySymbol fs && !fs.IsActive)
        fs.Activate();

      return (T)(object)match;
    }

    private static IEnumerable<T> GetElementTypes<T>(Type type, List<BuiltInCategory> categories)
    {
      var collector = new FilteredElementCollector(Doc);
      if (categories.Count > 0)
      {
        using var filter = new ElementMulticategoryFilter(categories);
        return collector.WhereElementIsElementType().OfClass(type).WherePasses(filter).Cast<T>();
      }
      return collector.WhereElementIsElementType().OfClass(type).Cast<T>();
    }
  }
}
