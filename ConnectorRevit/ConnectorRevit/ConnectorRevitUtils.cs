using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Logging;


namespace Speckle.ConnectorRevit
{
  public static class ConnectorRevitUtils
  {
#if REVIT2023
    public static string RevitAppName = VersionedHostApplications.Revit2023;
#elif REVIT2022
    public static string RevitAppName = VersionedHostApplications.Revit2022;
#elif REVIT2021
    public static string RevitAppName = VersionedHostApplications.Revit2021;
#elif REVIT2020
    public static string RevitAppName = VersionedHostApplications.Revit2020;
#else
    public static string RevitAppName = VersionedHostApplications.Revit2019;
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
          if (category == null)
            continue;
          //some categories, in other languages (eg DEU) have duplicated names #542
          if (_categories.ContainsKey(category.Name))
          {
            var spec = category.Id.ToString();
            if (category.Parent != null)
              spec = category.Parent.Name;
            _categories.Add(category.Name + " (" + spec + ")", category);
          }

          else
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

    public static List<string> GetWorksets(Document doc)
    {
      return new FilteredWorksetCollector(doc).Where(x => x.Kind == WorksetKind.UserWorkset).Select(x => x.Name).ToList();
    }

    private static async Task<List<string>> GetParameterNamesAsync(Document doc)
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

    private static async Task<List<string>> GetViewNamesAsync(Document doc)
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

    public static bool IsElementSupported(this Element e)
    {
      if (e.Category == null) return false;
      if (e.ViewSpecific) return false;

      if (SupportedBuiltInCategories.Contains((BuiltInCategory)e.Category.Id.IntegerValue))
        return true;
      return false;
    }



    //list of currently supported Categories (for sending only)
    //exact copy of the one in the ConverterRevitShared.Categories
    //until issue https://github.com/specklesystems/speckle-sharp/issues/392 is resolved
    private static List<BuiltInCategory> SupportedBuiltInCategories = new List<BuiltInCategory>{

      BuiltInCategory.OST_Areas,
      BuiltInCategory.OST_CableTray,
      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_Columns,
      BuiltInCategory.OST_CommunicationDevices,
      BuiltInCategory.OST_Conduit,
      BuiltInCategory.OST_CurtaSystem,
      BuiltInCategory.OST_DataDevices,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_DuctSystem,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctInsulations,
      BuiltInCategory.OST_ElectricalCircuit,
      BuiltInCategory.OST_ElectricalEquipment,
      BuiltInCategory.OST_ElectricalFixtures,
      BuiltInCategory.OST_Fascia,
      BuiltInCategory.OST_FireAlarmDevices,
      BuiltInCategory.OST_FlexDuctCurves,
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_Grids,
      BuiltInCategory.OST_Gutter,
      //BuiltInCategory.OST_HVAC_Zones,
      BuiltInCategory.OST_IOSModelGroups,
      BuiltInCategory.OST_LightingDevices,
      BuiltInCategory.OST_LightingFixtures,
      BuiltInCategory.OST_Lines,
      BuiltInCategory.OST_Mass,
      BuiltInCategory.OST_MassFloor,
      BuiltInCategory.OST_Materials,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_Parking,
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_PipingSystem,
      BuiltInCategory.OST_PointClouds,
      BuiltInCategory.OST_PointLoads,
      BuiltInCategory.OST_StairsRailing,
      BuiltInCategory.OST_RailingSupport,
      BuiltInCategory.OST_RailingTermination,
      BuiltInCategory.OST_Rebar,
      BuiltInCategory.OST_Roads,
      BuiltInCategory.OST_RoofSoffit,
      BuiltInCategory.OST_Roofs,
      BuiltInCategory.OST_Rooms,
      BuiltInCategory.OST_SecurityDevices,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_Site,
      BuiltInCategory.OST_EdgeSlab,
      BuiltInCategory.OST_Stairs,
      BuiltInCategory.OST_AreaRein,
      BuiltInCategory.OST_StructuralFramingSystem,
      BuiltInCategory.OST_StructuralColumns,
      BuiltInCategory.OST_StructConnections,
      BuiltInCategory.OST_FabricAreas,
      BuiltInCategory.OST_FabricReinforcement,
      BuiltInCategory.OST_StructuralFoundation,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_PathRein,
      BuiltInCategory.OST_StructuralStiffener,
      BuiltInCategory.OST_StructuralTruss,
      BuiltInCategory.OST_SwitchSystem,
      BuiltInCategory.OST_TelephoneDevices,
      BuiltInCategory.OST_Topography,
      BuiltInCategory.OST_Cornices,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_Wire,
      BuiltInCategory.OST_Casework,
      BuiltInCategory.OST_CurtainWallPanels,
      BuiltInCategory.OST_CurtainWallMullions,
      BuiltInCategory.OST_Entourage,
      BuiltInCategory.OST_Furniture,
      BuiltInCategory.OST_FurnitureSystems,
      BuiltInCategory.OST_Planting,
      BuiltInCategory.OST_PlumbingFixtures,
      BuiltInCategory.OST_Ramps,
      BuiltInCategory.OST_SpecialityEquipment,
      BuiltInCategory.OST_Rebar,
#if !REVIT2019 && !REVIT2020 && !REVIT2021
      BuiltInCategory.OST_AudioVisualDevices,
      BuiltInCategory.OST_FireProtection,
      BuiltInCategory.OST_FoodServiceEquipment,
      BuiltInCategory.OST_Hardscape,
      BuiltInCategory.OST_MedicalEquipment,
      BuiltInCategory.OST_Signage,
      BuiltInCategory.OST_TemporaryStructure,
      BuiltInCategory.OST_VerticalCirculation,
#endif
#if !REVIT2019 && !REVIT2020 && !REVIT2021 && !REVIT2022
       BuiltInCategory.OST_MechanicalControlDevices,
#endif
  };
  }
}
