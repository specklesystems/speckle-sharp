#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Classes;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using OSG = Objects.Structural.Geometry;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit : IRevitElementTypeRetriever<ElementType>
  {
    private ConversionOperationCache conversionOperationCache { get; } = new();
    public string? GetRevitTypeOfBase(Base @base)
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
      };
      return type ?? @base["type"] as string;
    }
    
    public void SetRevitTypeOfBase(Base @base, string type)
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
        default:
          @base["type"] = type;
          break;
      };
    }

    public string GetRevitCategoryOfBase(Base @base)
    {
      var elementTypeInfo = ElementTypeInfo.GetElementTypeInfoOfSpeckleObject(@base);
      return elementTypeInfo.CategoryName;
    }

    public bool CacheContainsTypeWithName(string baseType)
    {
      var type = conversionOperationCache.TryGet<ElementType>(baseType);
      if (type == null) return false;

      return true;
    }

    public IEnumerable<ElementType> GetAllCachedElementTypes()
    {
      return conversionOperationCache.GetAllObjectsOfType<ElementType>();
    }

    public IEnumerable<ElementType> GetAndCacheAvailibleTypes(Base @base)
    {
      var elementTypeInfo = ElementTypeInfo.GetElementTypeInfoOfSpeckleObject(@base);
      var types = conversionOperationCache.GetOrAdd<IEnumerable<ElementType>>(
        elementTypeInfo.CategoryName,
        () => GetElementTypes<ElementType>(elementTypeInfo.ElementTypeType, elementTypeInfo.BuiltInCategories),
        out var typesRetrieved);

      // if type was added instead of retreived, add types to master cache to facilitate lookup later
      if (!typesRetrieved)
      {
        conversionOperationCache.AddMany<ElementType>(types, type => type.Name);
      }

      return types;
    }

    //private T GetElementType<T>(Base element, ApplicationObject appObj, out bool isExactMatch)
    //{
    //  isExactMatch = false;
    //  var filter = GetCategoryFilter(element);
    //  var types = GetElementTypesThatPassFilter<T>(filter);

    //  if (types.Count == 0)
    //  {
    //    var name = typeof(T).Name;
    //    if (element["category"] is string category && !string.IsNullOrWhiteSpace(category))
    //      name = category;

    //    appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not find any loaded family to use for category {name}.");

    //    return default;
    //  }

    //  var family = (element["family"] as string)?.ToLower();
    //  var type = GetRevitTypeOfSpeckleObject(element)?.ToLower();

    //  return GetElementType<T>(element, family, type, types, appObj, out isExactMatch);
    //}

    //private T GetElementType<T>(Base element, string family, string type, List<ElementType> types, ApplicationObject appObj, out bool isExactMatch)
    //{
    //  isExactMatch = false;
    //  ElementType match = null;

    //  if (!string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(type))
    //  {
    //    match = types.FirstOrDefault(x => x.FamilyName?.ToLower() == family && x.Name?.ToLower() == type);
    //    isExactMatch = match != null;
    //  }

    //  //some elements only have one family so we didn't add such prop our schema
    //  if (match == null && string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(type))
    //  {
    //    match = types.FirstOrDefault(x => x.Name?.ToLower() == type);
    //    isExactMatch = match != null;
    //  }

    //  // match the type only for when we auto assign it
    //  if (match == null && !string.IsNullOrEmpty(type))
    //  {
    //    match = types.FirstOrDefault(x =>
    //    {
    //      var symbolTypeParam = x.get_Parameter(DB.BuiltInParameter.ELEM_TYPE_PARAM);
    //      var symbolTypeNameParam = x.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM);
    //      if (symbolTypeParam != null && symbolTypeParam.AsValueString()?.ToLower() == type)
    //        return true;
    //      else if (symbolTypeNameParam != null && symbolTypeNameParam.AsValueString()?.ToLower() == type)
    //        return true;
    //      return false;
    //    });
    //    isExactMatch = match != null;
    //  }

    //  if (match == null && !string.IsNullOrEmpty(family)) // try and match the family only.
    //  {
    //    match = types.FirstOrDefault(x => x.FamilyName?.ToLower() == family);
    //  }

    //  if (match == null) // okay, try something!
    //  {
    //    if (element is Objects.BuiltElements.Wall) // specifies the basic wall sub type as default
    //      match = types.Cast<WallType>().Where(o => o.Kind == WallKind.Basic).Cast<ElementType>().FirstOrDefault();
    //    match ??= types.First();
    //  }

    //  if (!isExactMatch)
    //    appObj.Update(logItem: $"Missing type. Family: {family} Type: {type}\nType was replaced with: {match.FamilyName}, {match.Name}");

    //  if (match is FamilySymbol fs && !fs.IsActive)
    //    fs.Activate();

    //  return (T)(object)match;
    //}

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
