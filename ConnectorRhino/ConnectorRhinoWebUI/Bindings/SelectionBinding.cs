using System;
using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings;

public class SelectionBinding : ISelectionBinding
{
  public string Name { get; set; } = "selectionBinding";
  public IBridge Parent { get; set; }

  private bool _selectionExpired;
  
  public SelectionBinding()
  {
    RhinoDoc.SelectObjects += (sender, args) => { _selectionExpired = true; };
    RhinoDoc.DeselectObjects += (sender, args) => { _selectionExpired = true; };
    RhinoDoc.DeselectAllObjects += (sender, args) => { _selectionExpired = true; };

    RhinoDoc.EndOpenDocumentInitialViewUpdate += (sender, args) =>
    {
      // Resets selection doc change
      Parent.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, new SelectionInfo());
    };
    
    RhinoApp.Idle += (sender, args) =>
    {
      if (!_selectionExpired) return;
      var selInfo = GetSelection();
      Parent.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, selInfo);
      _selectionExpired = false;
    };
  } 
 
  public SelectionInfo GetSelection()
  {
    var objects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
    var objectIds = objects.Select(o => o.Id.ToString()).ToList();
    var layerCount = objects.Select(o => o.Attributes.LayerIndex).Distinct().Count();
    var objectTypes = objects.Select(o => o.ObjectType.ToString()).Distinct().ToList();
    return new SelectionInfo
    {
      ObjectIds = objectIds,
      Summary = $"Selected {objectIds.Count} objects ({String.Join(", ", objectTypes)}) from {layerCount} layer{(layerCount != 1 ? "s" : "")}."
    };
  }

}
