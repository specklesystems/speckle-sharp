using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings;

public class RhinoEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds() => new(); // TODO

  public override bool CheckExpiry(string[] changedObjectIds) => true;
}

public class RhinoSelectionFilter : DirectSelectionSendFilter
{
  public override List<string> GetObjectIds() => SelectedObjectIds;

  public override bool CheckExpiry(string[] changedObjectIds) => SelectedObjectIds.Intersect(changedObjectIds).Any();
}
