using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Revit.Async;
using Speckle.Connectors.Utils.Reflection;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Connectors.DUI.Bindings;

internal class BasicConnectorBindingRevit : IBasicConnectorBinding
{
  // POC: name and bridge might be better for them to be protected props?
  public string Name { get; private set; }
  public IBridge Parent { get; private set; }

  protected readonly DocumentModelStore _store;
  protected readonly RevitContext _revitContext;
  private readonly RevitSettings _revitSettings;

  public BasicConnectorBindingRevit(
    DocumentModelStore store,
    RevitSettings revitSettings,
    IBridge parent,
    RevitContext revitContext
  )
  {
    Name = "baseBinding";
    Parent = parent;
    _store = store;
    _revitContext = revitContext;
    _revitSettings = revitSettings;
    Commands = new BasicConnectorBindingCommands(parent);
    // POC: event binding?
    _store.DocumentChanged += (_, _) =>
    {
      Commands.NotifyDocumentChanged();
    };
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
    if (_revitContext.UIApplication == null)
    {
      return null;
    }

    var doc = _revitContext.UIApplication.ActiveUIDocument.Document;

    return new DocumentInfo
    {
      Name = doc.Title,
      Id = doc.GetHashCode().ToString(),
      Location = doc.PathName
    };
  }

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model)
  {
    _store.Models.Add(model);
  }

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
    // POC: don't know if we can rely on storing the ActiveUIDocument, hence getting it each time
    var activeUIDoc =
      _revitContext.UIApplication?.ActiveUIDocument
      ?? throw new SpeckleException("Unable to retrieve active UI document");
    var doc = _revitContext.UIApplication.ActiveUIDocument.Document;

    SenderModelCard model = (SenderModelCard)_store.GetModelById(modelCardId);
    List<string> objectsIds = model.SendFilter.GetObjectIds();

    // POC: GetElementsFromDocument could be interfaced out, extension is cleaner
    List<ElementId> elementIds = doc.GetElements(objectsIds).Select(e => e.Id).ToList();

    if (elementIds.Count == 0)
    {
      Commands.SetModelError(modelCardId, new InvalidOperationException("No objects found to highlight."));
      return;
    }

    // UiDocument operations should be wrapped into RevitTask, otherwise doesn't work on other tasks.
    RevitTask.RunAsync(() =>
    {
      activeUIDoc.Selection.SetElementIds(elementIds);
      activeUIDoc.ShowElements(elementIds);
    });
  }

  public BasicConnectorBindingCommands Commands { get; }
}
