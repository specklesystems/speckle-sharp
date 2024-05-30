using System.Reflection;
using Autodesk.Revit.DB;
using Revit.Async;
using Speckle.Connectors.Utils.Reflection;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Utils;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Connectors.DUI.Bindings;

internal sealed class BasicConnectorBindingRevit : IBasicConnectorBinding
{
  // POC: name and bridge might be better for them to be protected props?
  public string Name { get; private set; }
  public IBridge Parent { get; private set; }

  private readonly DocumentModelStore _store;
  private readonly RevitContext _revitContext;
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

  public string GetSourceApplicationName() => _revitSettings.HostSlug.ToLower(); // POC: maybe not right place but... // ANOTHER POC: We should align this naming from somewhere in common DUI projects instead old structs. I know there are other POC comments around this

  public string GetSourceApplicationVersion() => _revitSettings.HostAppVersion; // POC: maybe not right place but...

  public DocumentInfo? GetDocumentInfo()
  {
    // POC: not sure why this would ever be null, is this needed?
    _revitContext.UIApplication.NotNull();

    var doc = _revitContext.UIApplication.ActiveUIDocument?.Document;
    if (doc is null)
    {
      return null;
    }

    if (doc.IsFamilyDocument)
    {
      return new DocumentInfo("", "", "") { Message = "Family environment files not supported by Speckle." };
    }

    var info = new DocumentInfo(doc.PathName, doc.Title, doc.GetHashCode().ToString());

    return info;
  }

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model) => _store.UpdateModel(model);

  public void RemoveModel(ModelCard model) => _store.RemoveModel(model);

  public void HighlightModel(string modelCardId)
  {
    // POC: don't know if we can rely on storing the ActiveUIDocument, hence getting it each time
    var activeUIDoc =
      _revitContext.UIApplication?.ActiveUIDocument
      ?? throw new SpeckleException("Unable to retrieve active UI document");

    SenderModelCard model = (SenderModelCard)_store.GetModelById(modelCardId);

    var elementIds = model.SendFilter.NotNull().GetObjectIds().Select(ElementId.Parse).ToList();
    if (elementIds.Count != 0)
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
