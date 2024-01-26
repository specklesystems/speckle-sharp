using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using RevitSharedResources.Extensions.SpeckleExtensions;
using RevitSharedResources.Helpers.Extensions;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevit.UI;

public partial class ConnectorBindingsRevit
{
  public override List<ISelectionFilter> GetSelectionFilters()
  {
    var categories = new List<string>();
    var viewFilters = new List<string>();

    var views = new List<string>();
    var schedules = new List<string>();
    var worksets = new List<string>();
    var projectInfo = new List<string> { "Project Info", "Levels", "Views 2D", "Views 3D", "Families & Types" };

    if (CurrentDoc != null)
    {
      //selectionCount = CurrentDoc.Selection.GetElementIds().Count();
      categories = revitDocumentAggregateCache
        .GetOrInitializeWithDefaultFactory<Category>()
        .GetAllKeys()
        .OrderBy(x => x)
        .ToList();

      //categories = ConnectorRevitUtils.GetCategoryNames(CurrentDoc.Document);
      viewFilters = ConnectorRevitUtils.GetViewFilterNames(CurrentDoc.Document);
      views = ConnectorRevitUtils.GetViewNames(CurrentDoc.Document);
      schedules = ConnectorRevitUtils.GetScheduleNames(CurrentDoc.Document);
      worksets = ConnectorRevitUtils.GetWorksets(CurrentDoc.Document);
    }

    var filters = new List<ISelectionFilter>
    {
      new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Sends all supported elements and project information."
      },
      new ManualSelectionFilter(),
      new ListSelectionFilter
      {
        Slug = "category",
        Name = "Category",
        Icon = "Category",
        Values = categories,
        Description = "Adds all elements belonging to the selected categories"
      },
      new ListSelectionFilter
      {
        Slug = "view",
        Name = "View",
        Icon = "RemoveRedEye",
        Values = views,
        Description = "Adds all objects visible in the selected views"
      },
    };

    if (schedules.Any())
    {
      filters.Add(
        new ListSelectionFilter
        {
          Slug = "schedule",
          Name = "Schedule",
          Icon = "Table",
          Values = schedules,
          Description = "Sends the selected schedule as a DataTable"
        }
      );
    }

    if (viewFilters.Any())
    {
      filters.Add(
        new ListSelectionFilter
        {
          Slug = "filter",
          Name = "Filters",
          Icon = "FilterList",
          Values = viewFilters,
          Description = "Adds all elements that pass the selected filters"
        }
      );
    }

    if (worksets.Any())
    {
      filters.Add(
        new ListSelectionFilter
        {
          Slug = "workset",
          Name = "Workset",
          Icon = "Group",
          Values = worksets,
          Description = "Adds all elements belonging to the selected workset"
        }
      );
    }

    filters.Add(
      new ListSelectionFilter
      {
        Slug = "project-info",
        Name = "Project Information",
        Icon = "Information",
        Values = projectInfo,
        Description = "Adds the selected project information such as levels, views and family names to the stream"
      }
    );

    return filters;
  }

  public override List<string> GetSelectedObjects()
  {
    if (CurrentDoc == null)
    {
      return new List<string>();
    }

    var selectedObjects = CurrentDoc.Selection
      .GetElementIds()
      .Select(id => CurrentDoc.Document.GetElement(id).UniqueId)
      .ToList();
    return selectedObjects;
  }

  public override List<string> GetObjectsInView()
  {
    if (CurrentDoc == null)
    {
      return new List<string>();
    }

    var collector = new FilteredElementCollector(
      CurrentDoc.Document,
      CurrentDoc.Document.ActiveView.Id
    ).WhereElementIsNotElementType();
    var elementIds = collector.ToElements().Select(el => el.UniqueId).ToList();

    return elementIds;
  }

  public override void SelectClientObjects(List<string> args, bool deselect = false)
  {
    var selection = args.Select(x => CurrentDoc.Document.GetElement(x))
      .Where(x => x != null && x.IsPhysicalElement())
      .Select(x => x.Id)
      ?.ToList();
    if (!selection.Any())
    {
      return;
    }

    //merge two lists
    if (!deselect)
    {
      var currentSelection = CurrentDoc.Selection.GetElementIds().ToList();
      selection = currentSelection.Union(selection).ToList();
    }

    CurrentDoc.Selection.SetElementIds(selection);
    CurrentDoc.ShowElements(selection);
  }

  private Dictionary<ElementId, Document> GetLinkedDocuments()
  {
    var linkedDocs = new Dictionary<ElementId, Document>();

    // Get settings and return empty list if we should not send linked models
    var sendLinkedModels = CurrentSettings?.FirstOrDefault(x => x.Slug == "linkedmodels-send") as CheckBoxSetting;
    if (sendLinkedModels == null || !sendLinkedModels.IsChecked)
    {
      return linkedDocs;
    }

    var linkedRVTs = new FilteredElementCollector(CurrentDoc.Document)
      .OfCategory(BuiltInCategory.OST_RvtLinks)
      .OfClass(typeof(RevitLinkInstance))
      .ToElements()
      .Cast<RevitLinkInstance>();
    foreach (var linkedRVT in linkedRVTs)
    {
      linkedDocs.Add(linkedRVT.Id, linkedRVT.GetLinkDocument());
    }

    return linkedDocs;
  }

  private static List<Element> FilterHiddenDesignOptions(List<Element> selection)
  {
    using var collector = new FilteredElementCollector(CurrentDoc.Document);
    var designOptionsExist = collector
      .OfClass(typeof(DesignOption))
      .Cast<DesignOption>()
      .Where(option => option.IsPrimary == false)
      .Any();

    if (!designOptionsExist)
    {
      return selection;
    }

    //Only include the Main Model objects and those part of a Primary Design Option
    //https://speckle.community/t/revit-design-option-settings-are-ignored-in-everything-stream/3182/8
    var activeDesignOption = DesignOption.GetActiveDesignOptionId(CurrentDoc.Document);
    if (activeDesignOption != ElementId.InvalidElementId)
    {
      selection = selection.Where(x => x.DesignOption == null || x.DesignOption.Id == activeDesignOption).ToList();
    }
    else
    {
      selection = selection.Where(x => x.DesignOption == null || x.DesignOption.IsPrimary).ToList();
    }

    return selection;
  }

  /// <summary>
  /// Given the filter in use by a stream returns the document elements that match it.
  /// </summary>
  /// <param name="filter"></param>
  /// <returns></returns>
  private List<Element> GetSelectionFilterObjects(ISpeckleConverter converter, ISelectionFilter filter)
  {
    var linkedDocs = GetLinkedDocuments();

    var allDocs = new List<Document> { CurrentDoc.Document };
    allDocs.AddRange(linkedDocs.Values);

    var selection = new List<Element>();
    try
    {
      switch (filter.Slug)
      {
        case "manual":
          return GetManualSelection(filter, linkedDocs);

        case "all":
          selection = GetEverything(linkedDocs);
          return FilterHiddenDesignOptions(selection);

        case "category":
          selection = GetSelectionByCategory(filter, allDocs);
          return FilterHiddenDesignOptions(selection);

        case "filter":
          selection = GetSelectionByFilter(filter, allDocs);
          return FilterHiddenDesignOptions(selection);

        case "view":
          var selectedViews = GetSelectedViews(filter);
          selection = GetSelectionByView(selectedViews, linkedDocs);
          if (selectedViews.Count == 1)
          {
            // if the user is sending a single view, then we pass it to the converter in order for the converter
            // to retreive element meshes that are specific to that view
            converter.SetContextDocument(selectedViews[0]);
            return selection;
          }
          else
          {
            return FilterHiddenDesignOptions(selection);
          }

        case "schedule":
          return GetSelectionBySchedule(filter);

        case "project-info":
          return GetSelectionByProjectInfo(filter);

        case "workset":
          selection = GetSelectionByWorkset(filter, allDocs);
          return FilterHiddenDesignOptions(selection);

        default:
          throw new SpeckleException($"Unknown ISelectionFilterSlug, {filter.Slug}");
      }
    }
    catch (Exception ex)
    {
      throw new SpeckleException(
        $"Method {nameof(GetSelectionFilterObjects)} threw an error of type {ex.GetType()}. Reason: {ex.Message}",
        ex
      );
    }
  }

  private static List<Element> GetManualSelection(ISelectionFilter filter, Dictionary<ElementId, Document> linkedDocs)
  {
    var selection = filter.Selection.Select(x => CurrentDoc.Document.GetElement(x)).Where(x => x != null).ToList();
    var selectedLinkedFiles = selection.Where(x => x is RevitLinkInstance).Cast<RevitLinkInstance>().ToList();

    foreach (var selectedLinkedFile in selectedLinkedFiles)
    {
      if (linkedDocs.ContainsKey(selectedLinkedFile.Id))
      {
        selection.AddRange(linkedDocs[selectedLinkedFile.Id].GetSupportedElements(revitDocumentAggregateCache));
      }
    }

    return selection;
  }

  private static List<Element> GetEverything(Dictionary<ElementId, Document> linkedDocs)
  {
    var currentDoc = CurrentDoc.Document;
    var selection = new List<Element>();
    //add these only for the current doc
    if (!currentDoc.IsFamilyDocument)
    {
      selection.Add(currentDoc.ProjectInformation);
    }
    else
    {
      //add for family document
      IList<ElementFilter> filters = new List<ElementFilter>()
      {
        new ElementClassFilter(typeof(GenericForm)),
        new ElementClassFilter(typeof(GeomCombination)),
      };
      selection.AddRange(
        new FilteredElementCollector(currentDoc).WherePasses(new LogicalOrFilter(filters)).ToElements()
      );
    }
    selection.AddRange(currentDoc.Views2D());
    selection.AddRange(currentDoc.Views3D());

    // We specifically exclude `TableView` elements (Schedules) until schedule extraction has been improved for performance.
    var elements = currentDoc.GetSupportedElements(revitDocumentAggregateCache).Where(e => e is not TableView);
    selection.AddRange(elements);
    selection.AddRange(currentDoc.GetSupportedTypes(revitDocumentAggregateCache));

    //and these for every linked doc
    foreach (var linkedDoc in linkedDocs.Values)
    {
      // We specifically exclude `TableView` elements (Schedules) until schedule extraction has been improved for performance.
      var linkedElements = linkedDoc.GetSupportedElements(revitDocumentAggregateCache).Where(e => e is not TableView);
      selection.AddRange(linkedElements); // includes levels
      selection.AddRange(linkedDoc.GetSupportedTypes(revitDocumentAggregateCache));
    }

    return selection;
  }

  private List<Element> GetSelectionByCategory(ISelectionFilter filter, List<Document> allDocs)
  {
    var selection = new List<Element>();
    var catFilter = filter as ListSelectionFilter;
    var catIds = new List<ElementId>();

    foreach (var cat in catFilter.Selection)
    {
      var revitCategory = revitDocumentAggregateCache.GetOrInitializeWithDefaultFactory<Category>().TryGet(cat);
      if (revitCategory == null)
      {
        continue;
      }

      catIds.Add(revitCategory.Id);
    }

    using var categoryFilter = new ElementMulticategoryFilter(catIds);

    foreach (var doc in allDocs)
    {
      using var collector = new FilteredElementCollector(doc);
      selection.AddRange(
        collector.WhereElementIsNotElementType().WhereElementIsViewIndependent().WherePasses(categoryFilter).ToList()
      );
    }
    return selection;
  }

  private static List<Element> GetSelectionByFilter(ISelectionFilter filter, List<Document> allDocs)
  {
    var selection = new List<Element>();
    var rvtFilters = filter as ListSelectionFilter;
    foreach (Document doc in allDocs)
    {
      List<Element> elements = new();
      var viewFilters = ConnectorRevitUtils.GetFilters(doc).Where(x => rvtFilters.Selection.Contains(x.Name));
      foreach (ParameterFilterElement filterElement in viewFilters)
      {
        ICollection<ElementId> cates = filterElement.GetCategories();
        IList<ElementFilter> eleFilters = new List<ElementFilter>();
        foreach (var cat in cates)
        {
          eleFilters.Add(new ElementCategoryFilter(cat));
        }
        var cateFilter = new LogicalOrFilter(eleFilters);
        ElementFilter elementFilter = filterElement.GetElementFilter();
        if (elementFilter != null)
        {
          elements.AddRange(
            new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              .WherePasses(cateFilter)
              .WherePasses(elementFilter)
              .ToList()
          );
        }
        else
        {
          elements.AddRange(
            new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              .WherePasses(cateFilter)
              .ToList()
          );
        }
      }
      if (elements.Count > 0)
      {
        selection.AddRange(elements.GroupBy(x => x.Id.IntegerValue).Select(x => x.First()).ToList());
      }
    }
    return selection;
  }

  private static List<Element> GetSelectionByView(List<View> views, Dictionary<ElementId, Document> linkedDocs)
  {
    var selection = new List<Element>();
    foreach (var view in views)
    {
      selection.Add(view);

      using var docCollector = new FilteredElementCollector(CurrentDoc.Document, view.Id);
      selection.AddRange(
        docCollector
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .Where(x => !selection.Any(s => s.UniqueId == x.UniqueId)) //exclude elements already added from other views
          .ToList()
      );

      foreach (var linkedDoc in linkedDocs)
      {
        if (linkedDoc.Value == null)
        {
          continue;
        }
        //from Revit 2024 onward we can query linked docs
        //for earlier versions we can't: https://github.com/specklesystems/speckle-sharp/issues/2829
#if !REVIT2020 && !REVIT2021 && !REVIT2022 && !REVIT2023
        using var linkedDocCollector = new FilteredElementCollector(CurrentDoc.Document, view.Id, linkedDoc.Key);
        selection.AddRange(
          linkedDocCollector
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            //.Where(x => x.IsPhysicalElement())
            .Where(x => !selection.Any(s => s.UniqueId == x.UniqueId)) //exclude elements already added from other views
            .ToList()
        );

#else
        //check if linked doc is visible in main doc
        var linkedObject = CurrentDoc.Document.GetElement(linkedDoc.Key);
        if (linkedObject.IsHidden(view))
        {
          continue;
        }

        //get ALL the linked model objects
        selection
          .AddRange(linkedDoc.Value.GetSupportedElements(revitDocumentAggregateCache)
          .Where(x => !selection.Any(s => s.UniqueId == x.UniqueId)));
#endif
      }
    }
    return selection;
  }

  private static List<View> GetSelectedViews(ISelectionFilter filter)
  {
    var selection = new List<Element>();
    var viewFilter = filter as ListSelectionFilter;
    using var collector = new FilteredElementCollector(CurrentDoc.Document);
    using var scheduleExclusionFilter = new ElementClassFilter(typeof(ViewSchedule), true);
    return collector
      .WhereElementIsNotElementType()
      .OfClass(typeof(View))
      .WherePasses(scheduleExclusionFilter)
      .Cast<View>()
      .Where(x => viewFilter.Selection.Contains(x.Title))
      .Where(x => !x.IsTemplate)
      .ToList();
  }

  private static List<Element> GetSelectionBySchedule(ISelectionFilter filter)
  {
    var selection = new List<Element>();
    var scheduleFilter = filter as ListSelectionFilter;

    using var collector = new FilteredElementCollector(CurrentDoc.Document);
    var schedules = collector
      .WhereElementIsNotElementType()
      .OfClass(typeof(ViewSchedule))
      .Where(x => scheduleFilter.Selection.Contains(x.Name));

    foreach (var schedule in schedules)
    {
      selection.Add(schedule);
    }
    return selection;
  }

  private static List<Element> GetSelectionByProjectInfo(ISelectionFilter filter)
  {
    var selection = new List<Element>();
    var projectInfoFilter = filter as ListSelectionFilter;

    if (projectInfoFilter.Selection.Contains("Project Info"))
    {
      selection.Add(CurrentDoc.Document.ProjectInformation);
    }

    if (projectInfoFilter.Selection.Contains("Views 2D"))
    {
      selection.AddRange(CurrentDoc.Document.Views2D());
    }

    if (projectInfoFilter.Selection.Contains("Views 3D"))
    {
      selection.AddRange(CurrentDoc.Document.Views3D());
    }

    if (projectInfoFilter.Selection.Contains("Levels"))
    {
      selection.AddRange(CurrentDoc.Document.Levels());
    }

    if (projectInfoFilter.Selection.Contains("Families & Types"))
    {
      selection.AddRange(CurrentDoc.Document.GetSupportedTypes(revitDocumentAggregateCache));
    }

    return selection;
  }

  private static List<Element> GetSelectionByWorkset(ISelectionFilter filter, List<Document> allDocs)
  {
    var selection = new List<Element>();
    var worksetFilter = filter as ListSelectionFilter;
    var worksets = new FilteredWorksetCollector(CurrentDoc.Document)
      .Where(x => worksetFilter.Selection.Contains(x.Name))
      .Select(x => x.Id)
      .ToList();
    foreach (var doc in allDocs)
    {
      using var collector = new FilteredElementCollector(doc);
      var elementWorksetFilters = new List<ElementFilter>();

      foreach (var w in worksets)
      {
        elementWorksetFilters.Add(new ElementWorksetFilter(w));
      }

      var worksetLogicalFilter = new LogicalOrFilter(elementWorksetFilters);
      selection.AddRange(collector.WherePasses(worksetLogicalFilter).ToElements().ToList());
    }
    return selection;
  }

  private static string GetStringValue(Parameter p)
  {
    string value = "";
    if (!p.HasValue)
    {
      return value;
    }

    if (string.IsNullOrEmpty(p.AsValueString()) && string.IsNullOrEmpty(p.AsString()))
    {
      return value;
    }

    if (!string.IsNullOrEmpty(p.AsValueString()))
    {
      return p.AsValueString().ToLowerInvariant();
    }
    else
    {
      return p.AsString().ToLowerInvariant();
    }
  }

  /// Processes the provided list of elements, applying specific validations and transformations based on the element type.
  /// </summary>
  /// <param name="selectedObjects">A collection of elements to process.</param>
  /// <returns>
  /// A collection of elements after applying the respective validations and transformations.
  /// </returns>
  /// <remarks>
  /// Current Validations and Transformations:
  /// - Zones are expanded into their constituent spaces.
  /// - [Add additional validations here as they are implemented.]
  /// </remarks>
  private static IEnumerable<Element> HandleSelectedObjectDescendants(IEnumerable<Element> selectedObjects)
  {
    // Handle the resolution of selected Elements to their convertable states here
    foreach (var element in selectedObjects)
    {
      switch (element)
      {
        case Autodesk.Revit.DB.Mechanical.Zone zone:
          foreach (var space in zone.Spaces.OfType<Autodesk.Revit.DB.Mechanical.Space>())
          {
            yield return space;
          }

          break;

        default:
          yield return element;
          break;
      }
    }
  }
}
