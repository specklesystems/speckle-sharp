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
  public string Name => "baseBinding";
  public IBridge Parent { get; }
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

  public void UpdateModel(ModelCard model) => _store.UpdateModel(model);

  public void RemoveModel(ModelCard model) => _store.RemoveModel(model);

  public void HighlightObjects(List<string> objectIds)
  {
    var objects = GetObjectsFromIds(objectIds);

    if (objects.rhinoObjects.Count == 0 && objects.groups.Count == 0)
    {
      throw new InvalidOperationException(
        "Highlighting RhinoObject is not successful.",
        new ArgumentException($"{objectIds} is not a valid id", nameof(objectIds))
      );
    }

    HighlightObjectsOnView(objects.rhinoObjects, objects.groups);
  }

  public void HighlightModel(string modelCardId)
  {
    var objectIds = new List<string>();
    var myModel = _store.GetModelById(modelCardId);

    if (myModel is SenderModelCard sender)
    {
      objectIds = sender.SendFilter.NotNull().GetObjectIds();
    }

    if (myModel is ReceiverModelCard receiver && receiver.BakedObjectIds != null)
    {
      objectIds = receiver.BakedObjectIds;
    }

    if (objectIds.Count == 0)
    {
      Commands.SetModelError(modelCardId, new OperationCanceledException("No objects found to highlight."));
      return;
    }

    var objects = GetObjectsFromIds(objectIds);

    RhinoDoc.ActiveDoc.Objects.UnselectAll();

    if (objects.rhinoObjects.Count == 0 && objects.groups.Count == 0)
    {
      Commands.SetModelError(modelCardId, new OperationCanceledException("No objects found to highlight."));
      return;
    }

    HighlightObjectsOnView(objects.rhinoObjects, objects.groups);
  }

  private (List<RhinoObject> rhinoObjects, List<Group> groups) GetObjectsFromIds(List<string> objectIds)
  {
    List<RhinoObject> rhinoObjects = objectIds
      .Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id)))
      .Where(o => o != null)
      .ToList();

    // POC: On receive we group objects if return multiple objects
    List<Group> groups = objectIds
      .Select((id) => RhinoDoc.ActiveDoc.Groups.FindId(new Guid(id)))
      .Where(o => o != null)
      .ToList();

    return (rhinoObjects, groups);
  }

  private void HighlightObjectsOnView(IReadOnlyList<RhinoObject> rhinoObjects, IReadOnlyList<Group> groups)
  {
    RhinoDoc.ActiveDoc.Objects.UnselectAll();
    List<RhinoObject> rhinoObjectsToSelect = new(rhinoObjects);

    foreach (Group group in groups)
    {
      int groupIndex = RhinoDoc.ActiveDoc.Groups.Find(group.Name);
      if (groupIndex < 0)
      {
        continue;
      }
      var allRhinoObjects = RhinoDoc.ActiveDoc.Objects.GetObjectList(ObjectType.AnyObject);
      var subRhinoObjects = allRhinoObjects.Where(o => o.GetGroupList().Contains(groupIndex));
      rhinoObjectsToSelect.AddRange(subRhinoObjects);
    }
    RhinoDoc.ActiveDoc.Objects.Select(rhinoObjectsToSelect.Select(o => o.Id));

    // Calculate the bounding box of the selected objects
    BoundingBox boundingBox = BoundingBoxExtensions.UnionRhinoObjects(rhinoObjectsToSelect);

    // Zoom to the calculated bounding box
    if (boundingBox.IsValid)
    {
      RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(boundingBox);
    }

    RhinoDoc.ActiveDoc.Views.Redraw();
  }
}
