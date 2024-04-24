using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.ArcGIS.Filters;

public class ArcGISSelectionFilter : DirectSelectionSendFilter
{
  public override List<string> GetObjectIds() => SelectedObjectIds;

  public override bool CheckExpiry(string[] changedObjectIds) => SelectedObjectIds.Intersect(changedObjectIds).Any();
}
