using System.Collections.Generic;
using DUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class RevitEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds()
  {
    return new List<string>();
    // throw new System.NotImplementedException();
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
    // TODO;
    return false;
  }
}
