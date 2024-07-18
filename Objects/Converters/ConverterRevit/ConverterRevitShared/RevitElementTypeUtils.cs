#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using OSG = Objects.Structural.Geometry;
using DB = Autodesk.Revit.DB;
using Speckle.Core.Logging;
using RevitSharedResources.Extensions.SpeckleExtensions;

namespace Objects.Converter.Revit;

/// <summary>
/// Functionality related to retrieving, setting, and caching <see cref="DB.ElementType"/>s. Implements <see cref="IRevitElementTypeRetriever"/> and <see cref="IAllRevitCategoriesExposer"/> in order for the connector to be able to access object info through the converter during mapping on receive.
/// </summary>
public partial class ConverterRevit : IRevitElementTypeRetriever, IAllRevitCategoriesExposer
{
  public IAllRevitCategories AllCategories => new AllRevitCategories(revitDocumentAggregateCache);

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
    }
    ;

    if (!string.IsNullOrEmpty(type))
    {
      return type;
    }

    return @base["type"] as string;
  }

  public void SetElementType(Base @base, string type)
  {
    switch (@base)
    {
      case OSG.Element1D el:
        if (el.property == null)
        {
          goto default;
        }

        el.property.name = type;
        break;
      case OSG.Element2D el:
        if (el.property == null)
        {
          goto default;
        }

        el.property.name = type;
        break;
      case Other.Revit.RevitInstance el:
        if (el.typedDefinition == null)
        {
          goto default;
        }

        el.typedDefinition.type = type;
        break;
      case BuiltElements.TeklaStructures.TeklaBeam el:
        if (el.profile == null)
        {
          goto default;
        }

        el.profile.name = type;
        break;
      default:
        @base["type"] = type;
        break;
    }
    ;
  }

  public string? GetElementFamily(Base @base)
  {
    string family = null;
    switch (@base)
    {
      case Other.Revit.RevitInstance el:
        family = el.typedDefinition?.family;
        break;
    }
    ;
    return family ?? @base["family"] as string;
  }

  public void SetElementFamily(Base @base, string family)
  {
    switch (@base)
    {
      case Other.Revit.RevitInstance el:
        if (el.typedDefinition == null)
        {
          goto default;
        }

        el.typedDefinition.family = family;
        break;
      default:
        @base["family"] = family;
        break;
    }
    ;
  }

  #endregion

  private T? GetElementType<T>(Base element, ApplicationObject appObj, out bool isExactMatch)
    where T : ElementType
  {
    isExactMatch = false;

    var type = GetElementType(element);
    if (type == null)
    {
      SpeckleLog.Logger.Warning(
        "Could not find valid incoming type for element of type {speckleType}",
        element.speckle_type
      );
      appObj.Update(logItem: $"Could not find valid incoming type for element of type \"{element.speckle_type}\"");
    }
    var typeInfo = AllCategories.GetRevitCategoryInfo<T>(element);

    if (revitDocumentAggregateCache is null)
    {
      return default;
    }

    var types = revitDocumentAggregateCache
      .GetOrInitializeWithDefaultFactory<List<ElementType>>()
      .GetOrAddGroupOfTypes(typeInfo);

    if (!types.Any())
    {
      var name = typeof(T).Name;
      if (element["category"] is string category && !string.IsNullOrWhiteSpace(category))
      {
        name = category;
      }

      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"Could not find any loaded family to use for category {name}."
      );

      return default;
    }

    var family = GetElementFamily(element);

    ElementType exactType = null;
    if (!string.IsNullOrWhiteSpace(family))
    {
      exactType = types
        .Where(t => string.Equals(t.FamilyName, family, StringComparison.CurrentCultureIgnoreCase))
        .Where(t => string.Equals(t.Name, type, StringComparison.CurrentCultureIgnoreCase))
        .FirstOrDefault();
    }
    exactType ??= revitDocumentAggregateCache
      .GetOrInitializeWithDefaultFactory<ElementType>()
      .TryGet(typeInfo.GetCategorySpecificTypeName(type));

    if (exactType != null)
    {
      isExactMatch = true;
      if (exactType is FamilySymbol fs && !fs.IsActive)
      {
        fs.Activate();
      }

      return (T)exactType;
    }

    return GetElementType<T>(element, family, type, types, appObj, out isExactMatch);
  }

  private T GetElementType<T>(
    Base element,
    string? family,
    string? type,
    List<ElementType> types,
    ApplicationObject appObj,
    out bool isExactMatch
  )
  {
    isExactMatch = false;
    ElementType match = null;

    if (!string.IsNullOrEmpty(family)) // try and match the family only.
    {
      match = types.FirstOrDefault(x => x.FamilyName?.ToLower() == family.ToLower());
    }

    if (match == null)
    {
      if (element is Objects.BuiltElements.Wall) // specifies the basic wall sub type as default
      {
        match = types.Cast<WallType>().Where(o => o.Kind == WallKind.Basic).Cast<ElementType>().FirstOrDefault();
      }

      match ??= types.First();
    }

    if (!isExactMatch)
    {
      appObj.Update(
        logItem: $"Missing type. Family: {family ?? "Unknown"} Type: {type ?? "Unknown"}\nType was replaced with: {match.FamilyName}, {match.Name}"
      );
    }

    if (match is FamilySymbol fs && !fs.IsActive)
    {
      fs.Activate();
    }

    return (T)(object)match;
  }
}
