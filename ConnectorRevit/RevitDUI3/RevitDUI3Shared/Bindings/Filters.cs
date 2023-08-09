using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3.Bindings;

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
