using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ConnectorRevit;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevit.UI
{
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
        categories = ConnectorRevitUtils.GetCategoryNames(CurrentDoc.Document);
        viewFilters = ConnectorRevitUtils.GetViewFilterNames(CurrentDoc.Document);
        views = ConnectorRevitUtils.GetViewNames(CurrentDoc.Document);
        schedules = ConnectorRevitUtils.GetScheduleNames(CurrentDoc.Document);
        worksets = ConnectorRevitUtils.GetWorksets(CurrentDoc.Document);
      }

      var filters = new List<ISelectionFilter>
      {
         new AllSelectionFilter {Slug="all",  Name = "Everything", Icon = "CubeScan", Description = "Sends all supported elements and project information." },
        new ManualSelectionFilter(),
        new ListSelectionFilter {Slug="category", Name = "Category", Icon = "Category", Values = categories, Description="Adds all elements belonging to the selected categories"},
        new ListSelectionFilter { Slug = "view", Name = "View", Icon = "RemoveRedEye", Values = views, Description = "Adds all objects visible in the selected views" },
      };

      if (schedules.Any())
        filters.Add(new ListSelectionFilter { Slug = "schedule", Name = "Schedule", Icon = "Table", Values = schedules, Description = "Sends the selected schedule as a DataTable" });

      if (viewFilters.Any())
        filters.Add(new ListSelectionFilter { Slug = "filter", Name = "Filters", Icon = "FilterList", Values = viewFilters, Description = "Adds all elements that pass the selected filters" });

      if (worksets.Any())
        filters.Add(new ListSelectionFilter { Slug = "workset", Name = "Workset", Icon = "Group", Values = worksets, Description = "Adds all elements belonging to the selected workset" });

      filters.Add(new ListSelectionFilter { Slug = "project-info", Name = "Project Information", Icon = "Information", Values = projectInfo, Description = "Adds the selected project information such as levels, views and family names to the stream" });



      return filters;
    }

    public override List<string> GetSelectedObjects()
    {
      if (CurrentDoc == null)
      {
        return new List<string>();
      }

      var selectedObjects = CurrentDoc.Selection.GetElementIds().Select(id => CurrentDoc.Document.GetElement(id).UniqueId).ToList();
      return selectedObjects;
    }

    public override List<string> GetObjectsInView()
    {
      if (CurrentDoc == null)
      {
        return new List<string>();
      }

      var collector = new FilteredElementCollector(CurrentDoc.Document, CurrentDoc.Document.ActiveView.Id).WhereElementIsNotElementType();
      var elementIds = collector.ToElements().Select(el => el.UniqueId).ToList(); ;

      return elementIds;
    }

    public override void SelectClientObjects(List<string> args, bool deselect = false)
    {
      var selection = args.Select(x => CurrentDoc.Document.GetElement(x)).Where(x => x != null && x.IsPhysicalElement()).Select(x => x.Id)?.ToList();
      if (!selection.Any())
        return;

      //merge two lists
      if (!deselect)
      {
        var currentSelection = CurrentDoc.Selection.GetElementIds().ToList();
        selection = currentSelection.Union(selection).ToList();
      }

      CurrentDoc.Selection.SetElementIds(selection);
      CurrentDoc.ShowElements(selection);


    }

    private List<Document> GetLinkedDocuments()
    {
      var docs = new List<Document>();

      // Get settings and return empty list if we should not send linked models
      var sendLinkedModels = CurrentSettings?.FirstOrDefault(x => x.Slug == "linkedmodels-send") as CheckBoxSetting;
      if (sendLinkedModels == null || !sendLinkedModels.IsChecked)
        return docs;


      //TODO: is the name the most safe way to look for it?
      var linkedRVTs = new FilteredElementCollector(CurrentDoc.Document).OfCategory(BuiltInCategory.OST_RvtLinks).OfClass(typeof(RevitLinkType)).ToElements().Cast<RevitLinkType>().Select(x => x.Name.Replace(".rvt", ""));
      foreach (Document revitDoc in RevitApp.Application.Documents)
      {
        if (revitDoc.IsLinked && linkedRVTs.Contains(revitDoc.Title))
        {
          docs.Add(revitDoc);
        }
      }

      return docs;
    }

    /// <summary>
    /// Given the filter in use by a stream returns the document elements that match it.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    private List<Element> GetSelectionFilterObjects(ISelectionFilter filter)
    {
      var currentDoc = CurrentDoc.Document;
      var allDocs = GetLinkedDocuments();
      allDocs.Add(currentDoc);

      var selection = new List<Element>();
      try
      {

        switch (filter.Slug)
        {

          case "manual":
            selection = filter.Selection.Select(x => CurrentDoc.Document.GetElement(x)).Where(x => x != null).ToList();
            var linkedFiles = selection.Where(x => x is RevitLinkInstance).Cast<RevitLinkInstance>().ToList();

            foreach (var linkedFile in linkedFiles)
            {
              var match = allDocs.FirstOrDefault(x => x.Title == linkedFile.Name.Split(new string[] { ".rvt" }, StringSplitOptions.None)[0]);
              if (match != null)
                selection.AddRange(match.SupportedElements());
            }

            return selection;

          case "all":
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
              selection.AddRange(new FilteredElementCollector(currentDoc).WherePasses(new LogicalOrFilter(filters)).ToElements());
            }
            selection.AddRange(currentDoc.Views2D());
            selection.AddRange(currentDoc.Views3D());

            //and these for every linked doc
            foreach (var doc in allDocs)
            {
              selection.AddRange(doc.SupportedElements()); // includes levels
              selection.AddRange(doc.SupportedTypes());
            }

            return selection;

          case "category":
            var catFilter = filter as ListSelectionFilter;
            var bics = new List<BuiltInCategory>();
            var categories = ConnectorRevitUtils.GetCategories(currentDoc);
            IList<ElementFilter> elementFilters = new List<ElementFilter>();

            foreach (var cat in catFilter.Selection)
            {
              elementFilters.Add(new ElementCategoryFilter(categories[cat].Id));
            }

            var categoryFilter = new LogicalOrFilter(elementFilters);
            foreach (var doc in allDocs)
            {
              selection.AddRange(new FilteredElementCollector(doc)
             .WhereElementIsNotElementType()
             .WhereElementIsViewIndependent()
             .WherePasses(categoryFilter).ToList());
            }
            return selection;
          case "filter":
            var rvtFilters = filter as ListSelectionFilter;
            foreach (Document doc in allDocs)
            {
              List<Element> elements = new List<Element>();
              var viewFilters = ConnectorRevitUtils.GetFilters(doc)
                .Where(x => rvtFilters.Selection.Contains(x.Name));
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
                  elements.AddRange(new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .WherePasses(cateFilter)
                    .WherePasses(elementFilter).ToList());
                }
                else
                {
                  elements.AddRange(new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .WherePasses(cateFilter)
                    .ToList());
                }

              }
              if (elements.Count > 0)
              {
                selection.AddRange(elements.GroupBy(x => x.Id.IntegerValue).Select(x => x.First()).ToList());
              }

            }
            return selection;
          case "view":
            var viewFilter = filter as ListSelectionFilter;

            var views = new FilteredElementCollector(currentDoc)
              .WhereElementIsNotElementType()
              .OfClass(typeof(View))
              .Where(x => viewFilter.Selection.Contains(x.Name));

            if (!views.Where(v => v is not ViewSchedule).Any())
            {
              foreach (var view in views)
              {
                selection.Add(view);
              }
              return selection;
            }

            foreach (var view in views)
            {
              selection.Add(view);
              var ids = selection.Select(x => x.UniqueId);

              foreach (var doc in allDocs)
              {
                selection.AddRange(new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                //.Where(x => x.IsPhysicalElement())
                .Where(x => !ids.Contains(x.UniqueId)) //exclude elements already added from other views
                .ToList());
              }
            }
            return selection;

          case "schedule":
            var scheduleFilter = filter as ListSelectionFilter;

            var schedules = new FilteredElementCollector(currentDoc)
              .WhereElementIsNotElementType()
              .OfClass(typeof(ViewSchedule))
              .Where(x => scheduleFilter.Selection.Contains(x.Name));

            foreach (var schedule in schedules)
            {
              selection.Add(schedule);
            }
            return selection;

          case "project-info":
            var projectInfoFilter = filter as ListSelectionFilter;

            if (projectInfoFilter.Selection.Contains("Project Info"))
              selection.Add(currentDoc.ProjectInformation);

            if (projectInfoFilter.Selection.Contains("Views 2D"))
              selection.AddRange(currentDoc.Views2D());

            if (projectInfoFilter.Selection.Contains("Views 3D"))
              selection.AddRange(currentDoc.Views3D());

            if (projectInfoFilter.Selection.Contains("Levels"))
              selection.AddRange(currentDoc.Levels());

            if (projectInfoFilter.Selection.Contains("Families & Types"))
              selection.AddRange(currentDoc.SupportedTypes());

            return selection;

          case "workset":
            var worksetFilter = filter as ListSelectionFilter;
            var worksets = new FilteredWorksetCollector(currentDoc).Where(x => worksetFilter.Selection.Contains(x.Name)).Select(x => x.Id).ToList();
            foreach (var doc in allDocs)
            {
              var collector = new FilteredElementCollector(doc);
              var elementWorksetFilters = new List<ElementFilter>();

              foreach (var w in worksets)
              {
                elementWorksetFilters.Add(new ElementWorksetFilter(w));
              }

              var worksetLogicalFilter = new LogicalOrFilter(elementWorksetFilters);
              selection.AddRange(collector.WherePasses(worksetLogicalFilter).ToElements().ToList());
            }
            return selection;

          case "param":
            try
            {
              foreach (var doc in allDocs)
              {
                var propFilter = filter as PropertySelectionFilter;
                var query = new FilteredElementCollector(doc)
                  .WhereElementIsNotElementType()
                  .WhereElementIsNotElementType()
                  .WhereElementIsViewIndependent()
                  .Where(x => x.IsPhysicalElement())
                  .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

                propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

                switch (propFilter.PropertyOperator)
                {
                  case "equals":
                    query = query.Where(fi =>
                      GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
                    break;
                  case "contains":
                    query = query.Where(fi =>
                      GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
                    break;
                  case "is greater than":
                    query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
                                                fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                                                fi.LookupParameter(propFilter.PropertyName)) >
                                              double.Parse(propFilter.PropertyValue));
                    break;
                  case "is less than":
                    query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
                                                fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                                                fi.LookupParameter(propFilter.PropertyName)) <
                                              double.Parse(propFilter.PropertyValue));
                    break;
                }

                selection.AddRange(query.ToList());
              }
            }
            catch (Exception ex)
            {
              SpeckleLog.Logger.Error(
                ex,
                "Swallowing exception in {methodName}: {exceptionMessage}",
                nameof(GetSelectionFilterObjects),
                ex.Message
              );
            }
            return selection;
        }
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Failed to filter objects");
      }

      return selection;

    }

    private string GetStringValue(Parameter p)
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
  }
}
