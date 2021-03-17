using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Logging;


namespace Speckle.ConnectorRevit
{
  public static class ConnectorRevitUtils
  {
#if REVIT2021
    public static string RevitAppName = Applications.Revit2021;
#elif REVIT2020
      public static string RevitAppName = Applications.Revit2020;
#else
      public static string RevitAppName = Applications.Revit2019;
#endif

    private static List<string> _cachedParameters = null;
    private static List<string> _cachedViews = null;
    public static List<SpeckleException> ConversionErrors { get; set; }

    private static Dictionary<string, Category> _categories { get; set; }

    public static Dictionary<string, Category> GetCategories(Document doc)
    {
      if (_categories == null)
      {
        _categories = new Dictionary<string, Category>();
        foreach (var bic in SupportedBuiltInCategories)
        {
          var category = Category.GetCategory(doc, bic);
          _categories.Add(category.Name, category);
        }
      }
      return _categories;
    }

    #region extension methods
    public static List<Element> SupportedElements(this Document doc)
    {
      //get element types of supported categories
      var categoryFilter = new LogicalOrFilter(GetCategories(doc).Select(x => new ElementCategoryFilter(x.Value.Id)).Cast<ElementFilter>().ToList());

      List<Element> elements = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .WherePasses(categoryFilter).ToList();

      return elements;
    }

    public static List<Element> SupportedTypes(this Document doc)
    {
      //get element types of supported categories
      var categoryFilter = new LogicalOrFilter(GetCategories(doc).Select(x => new ElementCategoryFilter(x.Value.Id)).Cast<ElementFilter>().ToList());

      List<Element> elements = new FilteredElementCollector(doc)
        .WhereElementIsElementType()
        .WherePasses(categoryFilter).ToList();

      return elements;
    }

    public static List<View> Views2D(this Document doc)
    {
      List<View> views = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .OfCategory(BuiltInCategory.OST_Views)
        .Cast<View>()
        .Where(x => x.ViewType == ViewType.CeilingPlan ||
        x.ViewType == ViewType.FloorPlan ||
        x.ViewType == ViewType.Elevation ||
        x.ViewType == ViewType.Section)
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
        .OfCategory(BuiltInCategory.OST_Levels).ToList();

      return levels;
    }
    #endregion

    public static List<string> GetCategoryNames(Document doc)
    {
      return GetCategories(doc).Keys.OrderBy(x => x).ToList();
    }

    private async static Task<List<string>> GetParameterNamesAsync(Document doc)
    {
      var els = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where(x => x.IsPhysicalElement());

      List<string> parameters = new List<string>();

      foreach (var e in els)
      {
        foreach (Parameter p in e.Parameters)
        {
          if (!parameters.Contains(p.Definition.Name))
            parameters.Add(p.Definition.Name);
        }
      }
      _cachedParameters = parameters.OrderBy(x => x).ToList();
      return _cachedParameters;
    }

    /// <summary>
    /// Each time it's called the cached parameters are returned, and a new copy is cached
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<string> GetParameterNames(Document doc)
    {
      if (_cachedParameters != null)
      {
        //don't wait for it to finish
        GetParameterNamesAsync(doc);
        return _cachedParameters;
      }
      return GetParameterNamesAsync(doc).Result;

    }

    private async static Task<List<string>> GetViewNamesAsync(Document doc)
    {
      var els = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .OfClass(typeof(View))
        .ToElements();

      _cachedViews = els.Select(x => x.Name).OrderBy(x => x).ToList();
      return _cachedViews;
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

    public static bool IsPhysicalElement(this Element e)
    {
      if (e.Category == null) return false;
      if (e.ViewSpecific) return false;
      // exclude specific unwanted categories
      if (((BuiltInCategory)e.Category.Id.IntegerValue) == BuiltInCategory.OST_HVAC_Zones) return false;
      return e.Category.CategoryType == CategoryType.Model && e.Category.CanAddSubcategory;
    }


    //list of currently supported Categories
    private static List<BuiltInCategory> SupportedBuiltInCategories = new List<BuiltInCategory>{

      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_Columns,
      BuiltInCategory.OST_CurtaSystem,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_Entourage,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_Furniture,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_Levels,
      BuiltInCategory.OST_Mass,
      BuiltInCategory.OST_Planting,
      BuiltInCategory.OST_Ramps,
      BuiltInCategory.OST_Roofs,
      BuiltInCategory.OST_Site,
      BuiltInCategory.OST_SpecialityEquipment,
      BuiltInCategory.OST_Stairs,
      BuiltInCategory.OST_StructuralColumns,
      BuiltInCategory.OST_StructuralFoundation,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_StructuralTruss,
      BuiltInCategory.OST_Topography,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Windows
    };
  }
}
