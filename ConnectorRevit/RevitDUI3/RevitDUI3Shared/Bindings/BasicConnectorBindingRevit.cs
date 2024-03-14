using System;
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

  public void AddModel(ModelCard model)
  {
    _store.Models.Add(model);
    _store.WriteToFile();
  }

  public void UpdateModel(ModelCard model)
  {
    int idx = _store.Models.FindIndex(m => model.ModelCardId == m.ModelCardId);
    _store.Models[idx] = model;
    _store.WriteToFile();
  }

  public void RemoveModel(ModelCard model)
  {
    int index = _store.Models.FindIndex(m => m.ModelCardId == model.ModelCardId);
    _store.Models.RemoveAt(index);
    _store.WriteToFile();
  }

  public void HighlightModel(string modelCardId)
  {
    // TODO: Support receivers
    var senderModelCard = _store.GetModelById(modelCardId) as SenderModelCard;

    var elementIds = senderModelCard.SendFilter.GetObjectIds().Select(ElementId.Parse).ToList();

    if (elementIds.Count == 0)
    {
      BasicConnectorBindingCommands.SetModelError(
        Parent,
        modelCardId,
        new OperationCanceledException("No objects found to highlight.")
      );
      return;
    }

    RevitTask.RunAsync(() =>
    {
      UiDocument.Selection.SetElementIds(elementIds);
      UiDocument.ShowElements(elementIds);
    });
  }
}
