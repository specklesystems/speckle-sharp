using System.Collections.Generic;
using System.Linq;
using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.Revit.Bindings;

public class RevitEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds()
  {
    // TODO
    return new List<string>();
  }

  public override bool CheckExpiry(string[] changedObjectIds)
  {
    return true;
  }
}

public class RevitSelectionFilter : DirectSelectionSendFilter
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
