using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;

namespace AutocadCivilDUI3Shared.Bindings
{
  public class AutocadEverythingFilter : EverythingSendFilter
  {
    public override bool CheckExpiry(string[] changedObjectIds)
    {
      return true;
    }

    public override List<string> GetObjectIds()
    {
      return new List<string>();
    }
  }
  public class AutocadSelectionFilter : DirectSelectionSendFilter
  {
    public override bool CheckExpiry(string[] changedObjectIds)
    {
      return SelectedObjectIds.Intersect(changedObjectIds).Any();
    }

    public override List<string> GetObjectIds()
    {
      return SelectedObjectIds;
    }
  }

}
