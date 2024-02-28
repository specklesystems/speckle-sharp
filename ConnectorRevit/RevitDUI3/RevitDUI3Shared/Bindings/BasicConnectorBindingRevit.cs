using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Revit.Async;
using Sentry.Reflection;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class BasicConnectorBindingRevit : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  // POC: statics should go
  private static UIApplication RevitApp { get; set; }
  private static UIDocument UiDocument => RevitApp.ActiveUIDocument;
  private static Document Doc => UiDocument.Document;

  private readonly RevitDocumentStore _store;

  public BasicConnectorBindingRevit(RevitDocumentStore store)
  {
    RevitApp = RevitAppProvider.RevitApp;
    _store = store;
    _store.DocumentChanged += (_, _) =>
    {
      BasicConnectorBindingCommands.NotifyDocumentChanged(Parent);
    };
  }

  public string GetConnectorVersion() => Assembly.GetAssembly(GetType()).GetNameAndVersion().Version;

  public string GetSourceApplicationName() => HostApplications.Revit.Slug;

  public string GetSourceApplicationVersion()
  {
#if REVIT2020
    return "2020";
#endif
#if REVIT2023
    return "2023";
#endif
  }

  public DocumentInfo GetDocumentInfo()
  {
    if (UiDocument == null)
    {
      return null;
    }

    return new DocumentInfo
    {
      Name = UiDocument.Document.Title,
      Id = UiDocument.Document.GetHashCode().ToString(),
      Location = UiDocument.Document.PathName
    };
  }

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model)
  {
    int idx = _store.Models.FindIndex(m => model.ModelCardId == m.ModelCardId);
    _store.Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    int index = _store.Models.FindIndex(m => m.ModelCardId == model.ModelCardId);
    _store.Models.RemoveAt(index);
  }

  public void HighlightModel(string modelCardId)
  {
    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    List<Element> elements = Utils.Elements.GetElementsFromDocument(Doc, objectsIds);

    List<ElementId> elementIds = elements.Select(e => e.Id).ToList();

    // UiDocument operations should be wrapped into RevitTask, otherwise doesn't work on other tasks.
    RevitTask.RunAsync(() =>
    {
      UiDocument.Selection.SetElementIds(elementIds);
      UiDocument.ShowElements(elementIds);

      // Create a BoundingBoxXYZ to encompass the selected elements
      BoundingBoxXYZ selectionBoundingBox = new();
      bool first = true;

      foreach (ElementId elementId in elementIds)
      {
        Element element = Doc.GetElement(elementId);

        if (element != null)
        {
          BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(null);

          if (elementBoundingBox != null)
          {
            if (first)
            {
              selectionBoundingBox = elementBoundingBox;
              first = false;
            }
            else
            {
              // selectionBoundingBox.Min = XYZ.Min(selectionBoundingBox.Min, elementBoundingBox.Min);
              // selectionBoundingBox.Max = XYZ.Max(selectionBoundingBox.Max, elementBoundingBox.Max);
            }
          }
        }
      }

      // Zoom the view to the selection bounding box
      if (!first)
      {
        View activeView = UiDocument.ActiveView;

        using Transaction tr = new(Doc, "Zoom to Selection");
        tr.Start();
        activeView.CropBox = selectionBoundingBox;
        Doc.Regenerate();
        tr.Commit();
      }
    });
  }
}
