using System.Collections.Generic;
using System.Linq;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using Rhino;
using Rhino.DocObjects;

namespace ConnectorRhinoWebUI.Bindings;

public class SelectionBinding : ISelectionBinding
{
  public string Name { get; set; } = "selectionBinding";
  public IBridge Parent { get; set; }

  public SelectionBinding()
  {
    RhinoDoc.SelectObjects += (_, _) =>
    {
      RhinoIdleManager.SubscribeToIdle(OnSelectionChanged);
    };
    RhinoDoc.DeselectObjects += (_, _) =>
    {
      RhinoIdleManager.SubscribeToIdle(OnSelectionChanged);
    };
    RhinoDoc.DeselectAllObjects += (_, _) =>
    {
      RhinoIdleManager.SubscribeToIdle(OnSelectionChanged);
    };

    RhinoDoc.EndOpenDocumentInitialViewUpdate += (_, _) =>
    {
      // Resets selection doc change
      Parent?.SendToBrowser(SelectionBindingEvents.SetSelection, new SelectionInfo());
    };
  }

  private void OnSelectionChanged()
  {
    SelectionInfo selInfo = GetSelection();
    Parent?.SendToBrowser(SelectionBindingEvents.SetSelection, selInfo);
  }

  public SelectionInfo GetSelection()
  {
    List<RhinoObject> objects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
    List<string> objectIds = objects.Select(o => o.Id.ToString()).ToList();
    int layerCount = objects.Select(o => o.Attributes.LayerIndex).Distinct().Count();
    List<string> objectTypes = objects.Select(o => o.ObjectType.ToString()).Distinct().ToList();
    return new SelectionInfo
    {
      SelectedObjectIds = objectIds,
      Summary =
        $"{objectIds.Count} objects ({string.Join(", ", objectTypes)}) from {layerCount} layer{(layerCount != 1 ? "s" : "")}"
    };
  }
}
