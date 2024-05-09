using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Sentry.Reflection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Rhino7.Extensions;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Utils;

namespace Speckle.Connectors.Rhino7.Bindings;

public class RhinoBasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  public BasicConnectorBindingCommands Commands { get; }

  private readonly DocumentModelStore _store;
  private readonly RhinoSettings _settings;

  public RhinoBasicConnectorBinding(DocumentModelStore store, RhinoSettings settings, IBridge parent)
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
    typeof(RhinoBasicConnectorBinding).Assembly.GetNameAndVersion().Version ?? "No version";

  public string GetSourceApplicationName() => _settings.HostAppInfo.Slug;

  public string GetSourceApplicationVersion() => "7";

  public DocumentInfo GetDocumentInfo() =>
    new(RhinoDoc.ActiveDoc.Path, RhinoDoc.ActiveDoc.Name, RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString());

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
    var objectIds = new List<string>();
    var myModel = _store.GetModelById(modelCardId);

    if (myModel is SenderModelCard sender)
    {
      objectIds = sender.SendFilter.NotNull().GetObjectIds();
    }

    if (myModel is ReceiverModelCard receiver && receiver.ReceiveResult != null)
    {
      objectIds = receiver.ReceiveResult.BakedObjectIds ?? new();
    }

    if (objectIds.Count == 0)
    {
      Commands.SetModelError(modelCardId, new OperationCanceledException("No objects found to highlight."));
      return;
    }

    List<RhinoObject> rhinoObjects = objectIds
      .Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id)))
      .Where(o => o != null)
      .ToList();

    RhinoDoc.ActiveDoc.Objects.UnselectAll();

    if (rhinoObjects.Count == 0)
    {
      Commands.SetModelError(modelCardId, new OperationCanceledException("No objects found to highlight."));
      return;
    }

    RhinoDoc.ActiveDoc.Objects.Select(rhinoObjects.Select(o => o.Id));

    // Calculate the bounding box of the selected objects
    BoundingBox boundingBox = BoundingBoxExtensions.UnionRhinoObjects(rhinoObjects);

    // Zoom to the calculated bounding box
    if (boundingBox.IsValid)
    {
      RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(boundingBox);
    }

    RhinoDoc.ActiveDoc.Views.Redraw();
  }
}
