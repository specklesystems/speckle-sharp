using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Other;
using Speckle.Core.Api;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using OSG = Objects.Structural.Geometry;

namespace ConverterRevitShared
{
  public partial class ConverterRevit
  {
    public static string GetRevitTypeOfSpeckleObject(Base @base)
    {
      var type = @base["type"] as string;

      // if the object is structural, we keep the type name in a different location
      if (@base is Objects.Structural.Geometry.Element1D element1D)
        type = element1D.property.name.Replace('X', 'x');
      else if (@base is Objects.Structural.Geometry.Element2D element2D)
        type = element2D.property.name;

      return type;
    }

    public static string GetRevitCategoryOfSpeckleObject(Base @base)
    {
      var elementTypeInfo = ElementTypeInfo.GetElementTypeInfoOfSpeckleObject(@base);
      return elementTypeInfo.CategoryName;
    }

    private IEnumerable<ElementType> GetAvailibleTypes(Base @base)
    {

    }

    private T GetElementType<T>(Base element, ApplicationObject appObj, out bool isExactMatch)
    {
      isExactMatch = false;
      var filter = GetCategoryFilter(element);
      var types = GetElementTypesThatPassFilter<T>(filter);

      if (types.Count == 0)
      {
        var name = typeof(T).Name;
        if (element["category"] is string category && !string.IsNullOrWhiteSpace(category))
          name = category;

        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not find any loaded family to use for category {name}.");

        return default;
      }

      var family = (element["family"] as string)?.ToLower();
      var type = GetRevitTypeOfSpeckleObject(element)?.ToLower();

      return GetElementType<T>(element, family, type, types, appObj, out isExactMatch);
    }

    private T GetElementType<T>(Base element, string family, string type, List<ElementType> types, ApplicationObject appObj, out bool isExactMatch)
    {
      isExactMatch = false;
      ElementType match = null;

      if (!string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(type))
      {
        match = types.FirstOrDefault(x => x.FamilyName?.ToLower() == family && x.Name?.ToLower() == type);
        isExactMatch = match != null;
      }

      //some elements only have one family so we didn't add such prop our schema
      if (match == null && string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(type))
      {
        match = types.FirstOrDefault(x => x.Name?.ToLower() == type);
        isExactMatch = match != null;
      }

      // match the type only for when we auto assign it
      if (match == null && !string.IsNullOrEmpty(type))
      {
        match = types.FirstOrDefault(x =>
        {
          var symbolTypeParam = x.get_Parameter(DB.BuiltInParameter.ELEM_TYPE_PARAM);
          var symbolTypeNameParam = x.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM);
          if (symbolTypeParam != null && symbolTypeParam.AsValueString()?.ToLower() == type)
            return true;
          else if (symbolTypeNameParam != null && symbolTypeNameParam.AsValueString()?.ToLower() == type)
            return true;
          return false;
        });
        isExactMatch = match != null;
      }

      if (match == null && !string.IsNullOrEmpty(family)) // try and match the family only.
      {
        match = types.FirstOrDefault(x => x.FamilyName?.ToLower() == family);
      }

      if (match == null) // okay, try something!
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

    private ElementFilter GetCategoryFilter(Base element)
    {
      switch (element)
      {
        case BuiltElements.Wall _:
          return new ElementMulticategoryFilter(Categories.wallCategories);
        case Column _:
          return new ElementMulticategoryFilter(Categories.columnCategories);
        case Beam _:
        case Brace _:
          return new ElementMulticategoryFilter(Categories.beamCategories);
        case Duct _:
          return new ElementMulticategoryFilter(Categories.ductCategories);
        case OSG.Element1D o:
          if (o.type == OSG.ElementType1D.Column)
            return new ElementMulticategoryFilter(Categories.columnCategories);
          else if (o.type == OSG.ElementType1D.Beam || o.type == OSG.ElementType1D.Brace)
            return new ElementMulticategoryFilter(Categories.beamCategories);
          else return null;
        case OSG.Element2D _:
        case Floor _:
          return new ElementMulticategoryFilter(Categories.floorCategories);
        case Pipe _:
          return new ElementMulticategoryFilter(Categories.pipeCategories);
        case Roof _:
          return new ElementCategoryFilter(BuiltInCategory.OST_Roofs);
        default:
          if (element["category"] != null)
          {
            var cat = Doc.Settings.Categories.Cast<Category>().FirstOrDefault(x => x.Name == element["category"].ToString());
            if (cat != null)
            {
              return new ElementCategoryFilter(cat.Id);
            }
          }
          return null;
      }
    }

    private List<ElementType> GetElementTypesThatPassFilter<T>(ElementFilter filter)
    {
      using var collector = new FilteredElementCollector(Doc);
      if (filter != null)
      {
        return collector.WhereElementIsElementType().OfClass(typeof(T)).WherePasses(filter).ToElements().Cast<ElementType>().ToList();
      }
      return collector.WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();
    }
  }
}
