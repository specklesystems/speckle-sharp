using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Sentry.Reflection;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Core.Credentials;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.Autocad.Utils;

namespace Speckle.Connectors.Autocad.Bindings;

public class AutocadBasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly AutocadSettings _settings;

  public BasicConnectorBindingCommands Commands { get; }

  private static Document Doc => Application.DocumentManager.MdiActiveDocument;

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

  public string GetConnectorVersion() => Assembly.GetAssembly(GetType()).GetNameAndVersion().Version;

  public string GetSourceApplicationName() => _settings.HostAppInfo.Slug;

  public string GetSourceApplicationVersion() => _settings.HostAppVersion.ToString();

  public Account[] GetAccounts() => AccountManager.GetAccounts().ToArray();

  public DocumentInfo GetDocumentInfo()
  {
    if (Doc == null)
    {
      return new DocumentInfo();
    }

    string name = Doc.Name.Split(System.IO.Path.PathSeparator).Reverse().First();
    return new DocumentInfo()
    {
      Name = name,
      Id = Doc.Name,
      Location = Doc.Name
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
    if (Doc == null)
    {
      return;
    }
    var objectIds = Array.Empty<ObjectId>();

    var model = _store.GetModelById(modelCardId);
    if (model == null)
    {
      return; // TODO: RECEIVERS
    }

    if (model is SenderModelCard senderModelCard)
    {
      List<(DBObject obj, string applicationId)> dbObjects = Doc.GetObjects(senderModelCard.SendFilter.GetObjectIds());

      objectIds = dbObjects.Select(tuple => tuple.obj.Id).ToArray();
    }

    // TODO: enable it when IReceiveBindingUICommands enabled!
    // if (
    //   model is ReceiverModelCard { ReceiveResult: { } } receiverModelCard
    //   && receiverModelCard.ReceiveResult.BakedObjectIds.Count != 0
    // )
    // {
    //   var dbObjects = GetObjectsFromDocument(Doc, receiverModelCard.ReceiveResult.BakedObjectIds);
    //   objectIds = dbObjects.Select(tuple => tuple.obj.Id).ToArray();
    // }

    if (objectIds.Length == 0)
    {
      Commands.SetModelError(modelCardId, new OperationCanceledException("No objects found to highlight."));
      return;
    }

    Parent.RunOnMainThread(() =>
    {
      Doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); // Deselects
      Doc.Editor.SetImpliedSelection(objectIds); // Selects
      Doc.Editor.UpdateScreen();

      Extents3d selectedExtents = new();
      var tr = Doc.TransactionManager.StartTransaction();
      foreach (ObjectId objectId in objectIds)
      {
        var entity = (Entity)tr.GetObject(objectId, OpenMode.ForRead);
        if (entity != null)
        {
          selectedExtents.AddExtents(entity.GeometricExtents);
        }
      }
      Doc.Editor.Zoom(selectedExtents); // TODO: It is extension method, re-consider?
      tr.Commit();
      Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
    });
  }
}
