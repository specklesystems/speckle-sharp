using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings;

public class RhinoEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds() => new List<string>(); // TODO

  public override bool CheckExpiry(string[] changedObjectIds) => true;
}

public class RhinoSelectionFilter : DirectSelectionSendFilter
{
  public override List<string> GetObjectIds() => SelectedObjectIds;

  public override bool CheckExpiry(string[] changedObjectIds) => SelectedObjectIds.Intersect(changedObjectIds).Any();
}

public class RhinoLayerFilter : ListValueInput, ISendFilter
{
  public RhinoLayerFilter()
  {
    Name = "Layers";
    Summary = "How many layers are actually selected. UI should populate this.";
    foreach (var layer in RhinoDoc.ActiveDoc.Layers)
    {
      if (layer.IsDeleted || layer.Disposed)
      {
        continue;
      }

      Options.Add(new ListValueItem { Id = layer.Id.ToString(), Name = layer.FullPath });
    }
  }

  public string Name { get; set; }
  public string Summary { get; set; }
  public bool IsDefault { get; set; }

  public List<string> GetObjectIds() => new List<string>(); // TODO: based on the SelectedOptions field

  public bool CheckExpiry(string[] changedObjectIds) =>
    // TODO
    false;
}
