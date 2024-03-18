using System.Collections.Generic;
using System.Linq;
using Speckle.Connectors.DUI.Bindings;

namespace Speckle.Connectors.Rhino7.Filters;

public class RhinoSelectionFilter : DirectSelectionSendFilter
{
  public override List<string> GetObjectIds() => SelectedObjectIds;

  public override bool CheckExpiry(string[] changedObjectIds) => SelectedObjectIds.Intersect(changedObjectIds).Any();
}
