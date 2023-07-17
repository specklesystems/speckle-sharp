using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.Filters;
using RevitSharedResources.Helpers.Extensions;

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

      if (viewFilters.Any())
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

      if (worksets.Any())
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
  }
}
