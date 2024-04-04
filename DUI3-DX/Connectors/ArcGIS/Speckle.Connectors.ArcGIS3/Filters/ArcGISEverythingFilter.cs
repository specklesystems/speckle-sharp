using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.ArcGIS.Filters;

public class ArcGISEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds() => new(); // TODO

  public override bool CheckExpiry(string[] changedObjectIds) => true;
}
