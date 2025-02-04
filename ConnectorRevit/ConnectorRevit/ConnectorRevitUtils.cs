using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using RevitSharedResources.Extensions.SpeckleExtensions;
using RevitSharedResources.Interfaces;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevit;

public static class ConnectorRevitUtils
{
#if REVIT2025
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2025);
#elif REVIT2024
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2024);
#elif REVIT2023
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2023);
#elif REVIT2022
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2022);
#elif REVIT2021
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2021);
#elif REVIT2020
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2020);
#elif REVIT2019
  public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2019);
#endif

  private static List<string> _cachedViews = null;
  private static List<string> _cachedScheduleViews = null;
  public static List<SpeckleException> ConversionErrors { get; set; }

  public static List<ParameterFilterElement> GetFilters(Autodesk.Revit.DB.Document doc)
  {
    return new FilteredElementCollector(doc)
      .OfClass(typeof(ParameterFilterElement))
      .OfType<ParameterFilterElement>()
      .OrderBy(x => x.Name)
      .ToList();
  }

  /// <summary>
  /// We want to display a user-friendly category names when grouping objects
  /// For this we are simplifying the BuiltIn one as otherwise, by using the display value, we'd be getting localized category names
  /// which would make querying etc more difficult
  /// TODO: deprecate this in favour of model collections
  /// </summary>
  /// <param name="category"></param>
  /// <returns></returns>
  public static string GetEnglishCategoryName(Category category)
  {
    var builtInCategory = (BuiltInCategory)category.Id.IntegerValue;
    var builtInCategoryName = builtInCategory
      .ToString()
      .Replace("OST_IOS", "") //for OST_IOSModelGroups
      .Replace("OST_MEP", "") //for OST_MEPSpaces
      .Replace("OST_", "") //for any other OST_blablabla
      .Replace("_", " ");
    builtInCategoryName = Regex.Replace(builtInCategoryName, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled).Trim();
    return builtInCategoryName;
  }

  #region extension methods

  public static List<Element> GetSupportedElements(this Document doc, IRevitDocumentAggregateCache cache)
  {
    //get elements of supported categories
    var categoryIds = cache
      .GetOrInitializeWithDefaultFactory<Category>()
      .GetAllObjects()
      .Select(category => category.Id)
      .ToList();

    using var categoryFilter = new ElementMulticategoryFilter(categoryIds);
    using var collector = new FilteredElementCollector(doc);

    var elements = collector
      .WhereElementIsNotElementType()
      .WhereElementIsViewIndependent()
      .WherePasses(categoryFilter)
      .ToList();

    return elements;
  }

  public static List<Element> GetSupportedTypes(this Document doc, IRevitDocumentAggregateCache cache)
  {
    //get element types of supported categories
    var categoryIds = cache
      .GetOrInitializeWithDefaultFactory<Category>()
      .GetAllObjects()
      .Select(category => category.Id)
      .ToList();

    using var categoryFilter = new ElementMulticategoryFilter(categoryIds);
    using var collector = new FilteredElementCollector(doc);

    var elements = collector.WhereElementIsElementType().WherePasses(categoryFilter).ToList();

    return elements;
  }

  public static List<View> Views2D(this Document doc)
  {
    List<View> views = new FilteredElementCollector(doc)
      .WhereElementIsNotElementType()
      .OfCategory(BuiltInCategory.OST_Views)
      .Cast<View>()
      .Where(x =>
        x.ViewType == ViewType.CeilingPlan
        || x.ViewType == ViewType.FloorPlan
        || x.ViewType == ViewType.Elevation
        || x.ViewType == ViewType.Section
      )
      .ToList();

    return views;
  }

  public static List<View> Views3D(this Document doc)
  {
    List<View> views = new FilteredElementCollector(doc)
      .WhereElementIsNotElementType()
      .OfCategory(BuiltInCategory.OST_Views)
      .Cast<View>()
      .Where(x => x.ViewType == ViewType.ThreeD)
      .ToList();

    return views;
  }

  public static List<Element> Levels(this Document doc)
  {
    List<Element> levels = new FilteredElementCollector(doc)
      .WhereElementIsNotElementType()
      .OfCategory(BuiltInCategory.OST_Levels)
      .ToList();

    return levels;
  }

  #endregion

  public static List<string> GetViewFilterNames(Document doc)
  {
    return GetFilters(doc).Select(x => x.Name).ToList();
  }

  public static List<string> GetWorksets(Document doc)
  {
    return new FilteredWorksetCollector(doc).Where(x => x.Kind == WorksetKind.UserWorkset).Select(x => x.Name).ToList();
  }

  private static async Task<List<string>> GetViewNamesAsync(Document doc)
  {
    using var scheduleExclusionFilter = new ElementClassFilter(typeof(ViewSchedule), true);
    var els = new FilteredElementCollector(doc)
      .WhereElementIsNotElementType()
      .OfClass(typeof(View))
      .WherePasses(scheduleExclusionFilter)
      .Cast<View>()
      .Where(x => !x.IsTemplate)
      .ToList();
    _cachedViews = els.Select(x => x.Title).OrderBy(x => x).ToList();
    return _cachedViews;
  }

  private static bool IsViewRevisionSchedule(string input)
  {
    string pattern = @"<.+>(\s*\d+)?";
    Regex rgx = new(pattern);
    return rgx.IsMatch(input);
  }

  private static async Task<List<string>> GetScheduleNamesAsync(Document doc)
  {
    var els = new FilteredElementCollector(doc)
      .WhereElementIsNotElementType()
      .OfClass(typeof(ViewSchedule))
      .Where(view => !IsViewRevisionSchedule(view.Name))
      .ToList();

    _cachedScheduleViews = els.Select(x => x.Name).OrderBy(x => x).ToList();
    return _cachedScheduleViews;
  }

  /// <summary>
  /// Each time it's called the cached parameters are return, and a new copy is cached
  /// </summary>
  /// <param name="doc"></param>
  /// <returns></returns>
  public static List<string> GetViewNames(Document doc)
  {
    if (_cachedViews != null)
    {
      //don't wait for it to finish
      GetViewNamesAsync(doc);
      return _cachedViews;
    }

    return GetViewNamesAsync(doc).Result;
  }

  public static List<string> GetScheduleNames(Document doc)
  {
    if (_cachedScheduleViews != null)
    {
      //don't wait for it to finish
      GetScheduleNamesAsync(doc);
      return _cachedScheduleViews;
    }

    return GetScheduleNamesAsync(doc).Result;
  }

  /// <summary>
  /// Removes all inherited classes from speckle type string
  /// </summary>
  /// <param name="s"></param>
  /// <returns></returns>
  public static string SimplifySpeckleType(string type)
  {
    return type.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
  }

  public static string ObjectDescriptor(Element obj)
  {
    var simpleType = obj.GetType()
      .ToString()
      .Split(new string[] { "DB." }, StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault();
    return string.IsNullOrEmpty(obj.Name) ? $"{simpleType}" : $"{simpleType} {obj.Name}";
  }
}
