using System.Collections.Generic;
using DUI3;
using DUI3.Models;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings;

public class RhinoEverythingFilter : SendFilter
{
  public RhinoEverythingFilter()
  {
    Name = "Everything";
    Summary = "All supported objects in the currently opened file.";
  }
  
  public override List<string> GetObjectIds()
  {
    return new List<string>(); // TODO
  }
}

public class RhinoSelectionFilter : DirectSelectionSendFilter
{
  public RhinoSelectionFilter()
  {
    Name = "Selection";
    Summary = "User based selection filter. UI should replace this summary with the selection info summary!";
  }
  
  public override List<string> GetObjectIds()
  {
    return SelectedObjectIds;
  }
}

public class RhinoLayerFilter : ListSendFilter // TODO: would be nicer as a tree send filter 🤔
{
  public RhinoLayerFilter()
  {
    Name = "Layers";
    Summary = "How many layers are actually selected. UI should populate this.";
    foreach (var layer in RhinoDoc.ActiveDoc.Layers)
    {
      Options.Add(layer.FullPath);
    }
  }
  
  public override List<string> GetObjectIds()
  {
    return new List<string>(); // TODO: based on the SelectedOptions field
  }
}

// NOTE: For fun, not implemented or implementable. It's meant to demo/test the case where we have multiple list based filters.
public class RhinoBlocksFilter : ListSendFilter
{
  public override List<string> GetObjectIds()
  {
    throw new System.NotImplementedException();
  }
}

