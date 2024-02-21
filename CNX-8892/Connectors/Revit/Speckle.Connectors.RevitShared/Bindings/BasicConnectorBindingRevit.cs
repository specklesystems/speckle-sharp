using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using Speckle.Connectors.Utils.Reflection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Revit.HostApp;

namespace Speckle.ConnectorRevitDUI3.Bindings;

internal class BasicConnectorBindingRevit : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  private readonly RevitDocumentStore _store;
  private readonly RevitSettings _revitSettings;
  private readonly UIApplication _uIApplication;

  public BasicConnectorBindingRevit(RevitDocumentStore store, RevitSettings revitSettings, IRevitPlugin revitPlugin)
  {
    _store = store;
    _revitSettings = revitSettings;
    _uIApplication = revitPlugin.UiApplication;

    // POC: event binding
    //_store.DocumentChanged += (_, _) =>
    //{
    //  Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
    //};
  }

  public string GetConnectorVersion()
  {
    return Assembly.GetAssembly(GetType()).GetVersion();
  }

  public string GetSourceApplicationName() => _revitSettings.HostSlug; // POC: maybe not right place but...

  public string GetSourceApplicationVersion()
  {
    // POC: maybe not right place but...
    return _revitSettings.HostAppVersion;
  }

  public DocumentInfo GetDocumentInfo()
  {
    // POC: not sure why this would ever be null, is this needed?
    if (_uIApplication == null)
    {
      return null;
    }

    var doc = _uIApplication.ActiveUIDocument.Document;

    return new DocumentInfo
    {
      Name = doc.Title,
      Id = doc.GetHashCode().ToString(),
      Location = doc.PathName
    };
  }

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model)
  {
    int idx = _store.Models.FindIndex(m => model.Id == m.Id);
    _store.Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    int index = _store.Models.FindIndex(m => m.Id == model.Id);
    _store.Models.RemoveAt(index);
  }

  public void HighlightModel(string modelCardId)
  {
    // POC: don't know if we can rely on storing the ActiveUIDocument, hence getting it each time
    var activeUIDoc = _uIApplication.ActiveUIDocument;
    var doc = _uIApplication.ActiveUIDocument.Document;

    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<string> objectsIds = model.SendFilter.GetObjectIds();

    // POC: GetElementsFromDocument could be interfaced out, extension is cleaner
    List<ElementId> elementIds = doc.GetElements(objectsIds).Select(e => e.Id).ToList();

    // UiDocument operations should be wrapped into RevitTask, otherwise doesn't work on other tasks.
    RevitTask.RunAsync(() =>
    {
      activeUIDoc.Selection.SetElementIds(elementIds);
      activeUIDoc.ShowElements(elementIds);

      // Create a BoundingBoxXYZ to encompass the selected elements
      BoundingBoxXYZ selectionBoundingBox = new();
      bool first = true;

      foreach (ElementId elementId in elementIds)
      {
        Element element = doc.GetElement(elementId);

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
        View activeView = activeUIDoc.ActiveView;

        using Transaction tr = new(doc, "Zoom to Selection");
        tr.Start();
        activeView.CropBox = selectionBoundingBox;
        doc.Regenerate();
        tr.Commit();
      }
    });
  }
}
