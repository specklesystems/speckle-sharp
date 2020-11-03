using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Speckle.Core.Logging;


namespace Speckle.ConnectorRevit
{
  public static class Globals
  {
    private static List<string> _cachedParameters = null;
    private static List<string> _cachedViews = null;
    public static List<SpeckleException> ConversionErrors { get; set; }


    private static Dictionary<string, Category> _categories { get; set; }

    public static Dictionary<string, Category> GetCategories(Document doc)
    {
      if (_categories == null)
      {
        _categories = new Dictionary<string, Category>();
        foreach (Category category in doc.Settings.Categories)
        {
          _categories.Add(category.Name, category);
        }
      }
      return _categories;
    }

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
    /// Each time it's called the cached parameters are return, and a new copy is cached
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
  }
}
