using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DesktopUI2.Models.Filters;
using RevitSharedResources.Helpers.Extensions;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorRevit.Services
{
  public class SendSelection : ISendSelection
  {
    private ISpeckleConverter converter;
    private UIDocument uiDocument;
    private ISelectionFilter filter;
    private IConversionSettings conversionSettings;
    private UIApplication uiApplication;

    public SendSelection(
      ISpeckleConverter converter,
      IEntityProvider<UIDocument> uiDocumentProvider,
      ISelectionFilter filter,
      IConversionSettings conversionSettings,
      UIApplication uiApplication
    )
    {
      this.converter = converter;
      this.uiDocument = uiDocumentProvider.Entity;
      this.filter = filter;
      this.conversionSettings = conversionSettings;
      this.uiApplication = uiApplication;
      GetSelectionFilterObjectsWithDesignOptions();
    }

    private Dictionary<string, Element> _selection;
    public ICollection<Element> Elements => _selection.Values;

    public bool ContainsElementWithId(string uniqueId)
    {
      return _selection.ContainsKey(uniqueId);
    }

    private void GetSelectionFilterObjectsWithDesignOptions()
    {
      var selection = GetSelectionFilterObjects();

      if (filter.Slug != "manual")
      {
        selection = FilterHiddenDesignOptions(selection);
      }

      _selection = selection.ToDictionary(
        element => element.UniqueId,
        element => element);
    }

    private List<Element> FilterHiddenDesignOptions(List<Element> selection)
    {
      using var collector = new FilteredElementCollector(uiDocument.Document);
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
      var activeDesignOption = DesignOption.GetActiveDesignOptionId(uiDocument.Document);
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
    private List<Element> GetSelectionFilterObjects()
    {
      var allDocs = GetLinkedDocuments();
      allDocs.Add(uiDocument.Document);

      var selection = new List<Element>();
      try
      {
        switch (filter.Slug)
        {
          case "manual":
            return GetManualSelection(allDocs);

          case "all":
            return GetEverything(allDocs);

          case "category":
            return GetSelectionByCategory(allDocs);

          case "filter":
            return GetSelectionByFilter(allDocs);

          case "view":
            return GetSelectionByView(allDocs);

          case "schedule":
            return GetScheduleSelection();

          case "project-info":
            return GetSelectionByProjectInfo();

          case "workset":
            return GetSelectionByWorkset(allDocs);

          case "param":
            return GetSelectionByParameter(allDocs, selection);
        }
      }
      catch (Exception ex)
      {
        throw new SpeckleException(
          $"Method {nameof(GetSelectionFilterObjects)} threw an error of type {ex.GetType()}. Reason: {ex.Message}",
          ex
        );
      }

      return selection;
    }

    private List<Document> GetLinkedDocuments()
    {
      var docs = new List<Document>();

      // Get settings and return empty list if we should not send linked models
      if (!conversionSettings.TryGetSettingBySlug("linkmodels-send", out var sendLinkedModels)
        || !bool.Parse(sendLinkedModels))
      {
        return docs;
      }

      //TODO: is the name the most safe way to look for it?
      var linkedRVTs = new FilteredElementCollector(uiDocument.Document)
        .OfCategory(BuiltInCategory.OST_RvtLinks)
        .OfClass(typeof(RevitLinkType))
        .ToElements()
      .Cast<RevitLinkType>()
        .Select(x => x.Name.Replace(".rvt", ""));
      foreach (Document revitDoc in uiApplication.Application.Documents)
      {
        if (revitDoc.IsLinked && linkedRVTs.Contains(revitDoc.Title))
        {
          docs.Add(revitDoc);
        }
      }

      return docs;
    }

    private List<Element> GetManualSelection(List<Document> allDocs)
    {
      var selection = filter.Selection.Select(x => uiDocument.Document.GetElement(x)).Where(x => x != null).ToList();
      var linkedFiles = selection.Where(x => x is RevitLinkInstance).Cast<RevitLinkInstance>().ToList();

      foreach (var linkedFile in linkedFiles)
      {
        var match = allDocs.FirstOrDefault(
          x => x.Title == linkedFile.Name.Split(new string[] { ".rvt" }, StringSplitOptions.None)[0]
        );
        if (match != null)
          selection.AddRange(match.SupportedElements());
      }

      return selection;
    }

    private List<Element> GetEverything(List<Document> allDocs)
    {
      var selection = new List<Element>();
      //add these only for the current doc
      if (!uiDocument.Document.IsFamilyDocument)
      {
        selection.Add(uiDocument.Document.ProjectInformation);
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
          new FilteredElementCollector(uiDocument.Document).WherePasses(new LogicalOrFilter(filters)).ToElements()
        );
      }
      selection.AddRange(uiDocument.Document.Views2D());
      selection.AddRange(uiDocument.Document.Views3D());

      //and these for every linked doc
      foreach (var doc in allDocs)
      {
        selection.AddRange(doc.SupportedElements()); // includes levels
        selection.AddRange(doc.SupportedTypes());
      }

      return selection;
    }

    private List<Element> GetSelectionByCategory(List<Document> allDocs)
    {
      var selection = new List<Element>();
      var catFilter = filter as ListSelectionFilter;
      var bics = new List<BuiltInCategory>();
      var categories = ConnectorRevitUtils.GetCategories(uiDocument.Document);
      IList<ElementFilter> elementFilters = new List<ElementFilter>();

      foreach (var cat in catFilter.Selection)
      {
        elementFilters.Add(new ElementCategoryFilter(categories[cat].Id));
      }

      var categoryFilter = new LogicalOrFilter(elementFilters);
      foreach (var doc in allDocs)
      {
        selection.AddRange(
          new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            .WherePasses(categoryFilter)
            .ToList()
        );
      }
      return selection;
    }

    private List<Element> GetSelectionByFilter(List<Document> allDocs)
    {
      var selection = new List<Element>();
      var rvtFilters = filter as ListSelectionFilter;
      foreach (Document doc in allDocs)
      {
        List<Element> elements = new List<Element>();
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

    private List<Element> GetSelectionByView(List<Document> allDocs)
    {
      var selection = new List<Element>();
      var viewFilter = filter as ListSelectionFilter;
      using var collector = new FilteredElementCollector(uiDocument.Document);
      using var scheduleExclusionFilter = new ElementClassFilter(typeof(ViewSchedule), true);
      var views = collector
        .WhereElementIsNotElementType()
        .OfClass(typeof(View))
        .WherePasses(scheduleExclusionFilter)
        .Cast<View>()
        .Where(x => viewFilter.Selection.Contains(x.Title))
        .Where(x => !x.IsTemplate)
        .ToList();

      // if the user is sending a single view, then we pass it to the converter in order for the converter
      // to retreive element meshes that are specific to that view
      if (views.Count == 1)
      {
        converter.SetContextDocument(views[0]);
      }

      foreach (var view in views)
      {
        selection.Add(view);
        var ids = selection.Select(x => x.UniqueId);

        foreach (var doc in allDocs)
        {
          using var docCollector = new FilteredElementCollector(doc, view.Id);
          selection.AddRange(
            docCollector
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              //.Where(x => x.IsPhysicalElement())
              .Where(x => !ids.Contains(x.UniqueId)) //exclude elements already added from other views
              .ToList()
          );
        }
      }
      return selection;
    }

    private List<Element> GetScheduleSelection()
    {
      var selection = new List<Element>();
      var scheduleFilter = filter as ListSelectionFilter;

      using var collector = new FilteredElementCollector(uiDocument.Document);
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

    private List<Element> GetSelectionByProjectInfo()
    {
      var selection = new List<Element>();
      var projectInfoFilter = filter as ListSelectionFilter;

      if (projectInfoFilter.Selection.Contains("Project Info"))
        selection.Add(uiDocument.Document.ProjectInformation);

      if (projectInfoFilter.Selection.Contains("Views 2D"))
        selection.AddRange(uiDocument.Document.Views2D());

      if (projectInfoFilter.Selection.Contains("Views 3D"))
        selection.AddRange(uiDocument.Document.Views3D());

      if (projectInfoFilter.Selection.Contains("Levels"))
        selection.AddRange(uiDocument.Document.Levels());

      if (projectInfoFilter.Selection.Contains("Families & Types"))
        selection.AddRange(uiDocument.Document.SupportedTypes());

      return selection;
    }

    private List<Element> GetSelectionByWorkset(List<Document> allDocs)
    {
      var selection = new List<Element>();
      var worksetFilter = filter as ListSelectionFilter;
      var worksets = new FilteredWorksetCollector(uiDocument.Document)
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

    private List<Element> GetSelectionByParameter(List<Document> allDocs, List<Element> selection)
    {
      try
      {
        foreach (var doc in allDocs)
        {
          var propFilter = filter as PropertySelectionFilter;
          using var collector = new FilteredElementCollector(doc);
          var query = collector
            .WhereElementIsNotElementType()
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            .Where(x => x.IsPhysicalElement())
            .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

          propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

          switch (propFilter.PropertyOperator)
          {
            case "equals":
              query = query.Where(
                fi => GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue
              );
              break;
            case "contains":
              query = query.Where(
                fi => GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue)
              );
              break;
            case "is greater than":
              query = query.Where(
                fi =>
                  RevitVersionHelper.ConvertFromInternalUnits(
                    fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                    fi.LookupParameter(propFilter.PropertyName)
                  ) > double.Parse(propFilter.PropertyValue)
              );
              break;
            case "is less than":
              query = query.Where(
                fi =>
                  RevitVersionHelper.ConvertFromInternalUnits(
                    fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                    fi.LookupParameter(propFilter.PropertyName)
                  ) < double.Parse(propFilter.PropertyValue)
              );
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
  }
}
