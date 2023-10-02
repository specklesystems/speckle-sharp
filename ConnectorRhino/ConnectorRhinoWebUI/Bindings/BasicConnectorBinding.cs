using System;
using System.Collections.Generic;
using System.Linq;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ConnectorRhinoWebUI.Bindings;

public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private readonly RhinoDocumentStore _store;

  public BasicConnectorBinding(RhinoDocumentStore store)
  {
    _store = store;
    _store.DocumentChanged += (_,_) =>
    {
      Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
    };
  }

  public string GetSourceApplicationName()
  {
    return "Rhino";
  }

  public string GetSourceApplicationVersion()
  {
    return "7";
  }

  public DocumentInfo GetDocumentInfo()
  {
    return new DocumentInfo
    {
      Location = RhinoDoc.ActiveDoc.Path,
      Name = RhinoDoc.ActiveDoc.Name,
      Id = RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString()
    };
  }

  public DocumentModelStore GetDocumentState()
  {
    return _store;
  }

  public void AddModel(ModelCard model)
  {
    _store.Models.Add(model);
  }

  public void UpdateModel(ModelCard model)
  {
     var idx = _store.Models.FindIndex(m => model.Id == m.Id);
    _store.Models[idx] = model;
  }
  
  public void RemoveModel(ModelCard model)
  {
    var index = _store.Models.FindIndex(m => m.Id == model.Id);
    _store.Models.RemoveAt(index);
  }

  public void HighlightModel(string modelCardId)
  {
    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    List<RhinoObject> rhinoObjects = objectsIds.Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id))).ToList();

    RhinoDoc.ActiveDoc.Objects.UnselectAll();
    foreach (var rhinoObject in rhinoObjects)
    {
      RhinoDoc.ActiveDoc.Objects.Select(rhinoObject.Id);
    }
    
    // Calculate the bounding box of the selected objects
    BoundingBox boundingBox = BoundingBox.Unset;
    foreach (var obj in rhinoObjects)
    {
      BoundingBox objBoundingBox = obj.Geometry.GetBoundingBox(false);
      if (objBoundingBox.IsValid)
      {
        if (boundingBox.IsValid)
          boundingBox.Union(objBoundingBox);
        else
          boundingBox = objBoundingBox;
      }
    }
    
    // Zoom to the calculated bounding box
    if (boundingBox.IsValid)
    {
      RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(boundingBox);
    }

    RhinoDoc.ActiveDoc.Views.Redraw();
  }
}
