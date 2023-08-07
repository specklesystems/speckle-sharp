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

  /// <summary>
  /// Gets event handler of <see cref="OnIdle(object, EventArgs)"/>.
  /// </summary>
  private EventHandler? idle;

  public SelectionBinding()
  {
    RhinoDoc.SelectObjects += (sender, args) => { this.EnableIdle(); };
    RhinoDoc.DeselectObjects += (sender, args) => { this.EnableIdle(); };
    RhinoDoc.DeselectAllObjects += (sender, args) => { this.EnableIdle(); };

    RhinoDoc.EndOpenDocumentInitialViewUpdate += (sender, args) =>
    {
      // Resets selection doc change
      Parent?.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, new SelectionInfo());
    };
  }

  /// <summary>
  /// Enables idle event.
  /// </summary>
  public void EnableIdle()
  {
    if (this.idle == null)
    {
      RhinoApp.Idle += this.idle = this.OnIdle;
    }
  }

  /// <summary>
  /// Disables idle event.
  /// </summary>
  private void DisableIdle()
  {
    if (this.idle != null)
    {
      RhinoApp.Idle -= this.idle;
      this.idle = null;
    }
  }

  /// <inheritdoc cref="RhinoApp.Idle"/>
  private void OnIdle(object sender, EventArgs e)
  {
    // Disable idle event handler until enabled by others.
    this.DisableIdle();

    var selInfo = GetSelection();
    Parent?.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, selInfo);
  }

  public SelectionInfo GetSelection()
    {
    var objects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
    var objectIds = objects.Select(o => o.Id.ToString()).ToList();
    var layerCount = objects.Select(o => o.Attributes.LayerIndex).Distinct().Count();
    var objectTypes = objects.Select(o => o.ObjectType.ToString()).Distinct().ToList();
    return new SelectionInfo
    {
      SelectedObjectIds = objectIds,
      Summary = $"{objectIds.Count} objects ({String.Join(", ", objectTypes)}) from {layerCount} layer{(layerCount != 1 ? "s" : "")}"
    };
  }
}
