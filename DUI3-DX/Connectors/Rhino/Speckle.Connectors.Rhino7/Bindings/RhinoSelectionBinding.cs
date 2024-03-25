using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Rhino7.HostApp;

namespace Speckle.Connectors.Rhino7.Bindings;

public class RhinoSelectionBinding : ISelectionBinding
{
  private const string SELECTION_EVENT = "setSelection";

  public string Name { get; } = "selectionBinding";
  public IBridge Parent { get; set; }

  public RhinoSelectionBinding(RhinoIdleManager idleManager, IBridge parent)
  {
    Parent = parent;

    RhinoDoc.SelectObjects += (_, _) =>
    {
      idleManager.SubscribeToIdle(OnSelectionChanged);
    };
    RhinoDoc.DeselectObjects += (_, _) =>
    {
      idleManager.SubscribeToIdle(OnSelectionChanged);
    };
    RhinoDoc.DeselectAllObjects += (_, _) =>
    {
      idleManager.SubscribeToIdle(OnSelectionChanged);
    };

    RhinoDoc.EndOpenDocumentInitialViewUpdate += (_, _) =>
    {
      // Resets selection doc change
      Parent?.Send(SELECTION_EVENT, new SelectionInfo());
    };
  }

  private void OnSelectionChanged()
  {
    SelectionInfo selInfo = GetSelection();
    Parent?.Send(SELECTION_EVENT, selInfo);
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
