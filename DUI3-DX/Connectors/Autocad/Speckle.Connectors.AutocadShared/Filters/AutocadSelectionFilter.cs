using Speckle.Connectors.DUI.Bindings;

namespace Speckle.Connectors.Autocad.Filters;

public class AutocadSelectionFilter : DirectSelectionSendFilter
{
  public override List<string> GetObjectIds() => SelectedObjectIds;

  public override bool CheckExpiry(string[] changedObjectIds) => SelectedObjectIds.Intersect(changedObjectIds).Any();
}
