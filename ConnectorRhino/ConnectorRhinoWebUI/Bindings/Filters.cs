using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Utils;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings;

public class RhinoEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds()
  {
    return new List<string>(); // TODO
  }

  public override bool CheckExpiry(string[] changedObjectIds)
  {
    return true;
  }
}

public class RhinoSelectionFilter : DirectSelectionSendFilter
{
  public override List<string> GetObjectIds()
  {
    return SelectedObjectIds;
  }

  public override bool CheckExpiry(string[] changedObjectIds)
  {
    return SelectedObjectIds.Intersect(changedObjectIds).Any();
  }
}

public class RhinoLayerFilter : ListValueInput, ISendFilter 
{
  public string Name { get; set; }
  public string Summary { get; set; }

  public RhinoLayerFilter()
  {
    Name = "Layers";
    Summary = "How many layers are actually selected. UI should populate this.";
    foreach (var layer in RhinoDoc.ActiveDoc.Layers)
    {
      Options.Add(new ListValueItem()
      {
        Id = layer.Id.ToString(),
        Name = layer.FullPath
      });
    }
  }
  
  public List<string> GetObjectIds()
  {
    return new List<string>(); // TODO: based on the SelectedOptions field
  }

  public bool CheckExpiry(string[] changedObjectIds)
  {
    // TODO
    return false;
  }
}

