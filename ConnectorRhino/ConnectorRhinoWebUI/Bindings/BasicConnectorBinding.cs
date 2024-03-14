using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ConnectorRhinoWebUI.Extensions;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Sentry.Reflection;

namespace ConnectorRhinoWebUI.Bindings;

public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private readonly RhinoDocumentStore _store;

  public BasicConnectorBinding(RhinoDocumentStore store)
  {
    _store = store;
    _store.DocumentChanged += (_, _) =>
    {
      BasicConnectorBindingCommands.NotifyDocumentChanged(Parent);
    };
  }

  public string GetConnectorVersion() => Assembly.GetAssembly(GetType()).GetNameAndVersion().Version;

  public string GetSourceApplicationName() => "Rhino";

  public string GetSourceApplicationVersion() => "7";

  public DocumentInfo GetDocumentInfo() =>
    new()
    {
      Location = RhinoDoc.ActiveDoc.Path,
      Name = RhinoDoc.ActiveDoc.Name,
      Id = RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString()
    };

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
      objectIds = sender.SendFilter.GetObjectIds();
    }

    if (myModel is ReceiverModelCard receiver && receiver.ReceiveResult != null)
    {
      objectIds = receiver.ReceiveResult.BakedObjectIds;
    }

    if (objectIds.Count == 0)
    {
      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId,
        new OperationCanceledException("No objects found to highlight."));
      return;
    }

    List<RhinoObject> rhinoObjects = objectIds
      .Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id))).Where(o => o != null)
      .ToList();

    RhinoDoc.ActiveDoc.Objects.UnselectAll();

    if (rhinoObjects.Count == 0)
    {
      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId,
        new OperationCanceledException("No objects found to highlight."));
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
