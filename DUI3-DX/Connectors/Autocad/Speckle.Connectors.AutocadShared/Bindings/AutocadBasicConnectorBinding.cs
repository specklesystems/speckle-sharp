using Autodesk.AutoCAD.DatabaseServices;
using Sentry.Reflection;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Core.Credentials;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.Utils;

namespace Speckle.Connectors.Autocad.Bindings;

public class AutocadBasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly AutocadSettings _settings;

  public BasicConnectorBindingCommands Commands { get; }

  public AutocadBasicConnectorBinding(DocumentModelStore store, AutocadSettings settings, IBridge parent)
  {
    _store = store;
    _settings = settings;
    Parent = parent;
    Commands = new BasicConnectorBindingCommands(parent);
    _store.DocumentChanged += (_, _) =>
    {
      Commands.NotifyDocumentChanged();
    };
  }

  public string GetConnectorVersion() =>
    typeof(AutocadBasicConnectorBinding).Assembly.GetNameAndVersion().Version ?? "No version";

  public string GetSourceApplicationName() => _settings.HostAppInfo.Slug;

  public string GetSourceApplicationVersion() => _settings.HostAppVersion.ToString();

  public Account[] GetAccounts() => AccountManager.GetAccounts().ToArray();

  public DocumentInfo? GetDocumentInfo()
  {
    // POC: Will be addressed to move it into AutocadContext!
    var doc = Application.DocumentManager.MdiActiveDocument;
    if (doc is null)
    {
      return null;
    }
    string name = doc.Name.Split(System.IO.Path.PathSeparator).Last();
    return new DocumentInfo(doc.Name, name, doc.GetHashCode().ToString());
  }

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model) => _store.UpdateModel(model);

  public void RemoveModel(ModelCard model) => _store.RemoveModel(model);

  public void HighlightModel(string modelCardId)
  {
    // POC: Will be addressed to move it into AutocadContext!
    var doc = Application.DocumentManager.MdiActiveDocument;

    if (doc == null)
    {
      return;
    }

    var objectIds = Array.Empty<ObjectId>();

    var model = _store.GetModelById(modelCardId);
    if (model == null)
    {
      return;
    }

    if (model is SenderModelCard senderModelCard)
    {
      List<(DBObject obj, string applicationId)> dbObjects = doc.GetObjects(
        senderModelCard.SendFilter.NotNull().GetObjectIds()
      );
      objectIds = dbObjects.Select(tuple => tuple.obj.Id).ToArray();
    }

    if (model is ReceiverModelCard receiverModelCard)
    {
      List<(DBObject obj, string applicationId)> dbObjects = doc.GetObjects(
        (receiverModelCard.ReceiveResult?.BakedObjectIds).NotNull()
      );
      objectIds = dbObjects.Select(tuple => tuple.obj.Id).ToArray();
    }

    if (objectIds.Length == 0)
    {
      Commands.SetModelError(modelCardId, new OperationCanceledException("No objects found to highlight."));
      return;
    }

    Parent.RunOnMainThread(() =>
    {
      doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); // Deselects
      doc.Editor.SetImpliedSelection(objectIds); // Selects
      doc.Editor.UpdateScreen();

      Extents3d selectedExtents = new();

      var tr = doc.TransactionManager.StartTransaction();
      foreach (ObjectId objectId in objectIds)
      {
        var entity = (Entity)tr.GetObject(objectId, OpenMode.ForRead);
        if (entity != null)
        {
          selectedExtents.AddExtents(entity.GeometricExtents);
        }
      }

      doc.Editor.Zoom(selectedExtents);
      tr.Commit();
      Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
    });
  }
}
