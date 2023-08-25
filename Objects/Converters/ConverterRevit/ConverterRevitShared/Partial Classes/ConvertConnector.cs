using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public RevitMEPConnector ConnectorToSpeckle(Connector connector)
    {
      var speckleMEPConnector = new RevitMEPConnector
      {
        applicationId = connector.GetUniqueApplicationId(),
        origin = PointToSpeckle(connector.Origin, Doc),
        shape = connector.Shape.ToString(),
        systemName = connector.MEPSystem?.Name ?? connector.Owner.Category?.Name,
      };

      // some genius at Autodesk thought it would be a good idea for property getters to throw...
      try
      {
        speckleMEPConnector.angle = connector.Angle;
      }
      catch { }
      try
      {
        speckleMEPConnector.height = ScaleToSpeckle(connector.Height);
        speckleMEPConnector.width = ScaleToSpeckle(connector.Width);
      }
      catch { }
      try
      {
        speckleMEPConnector.radius = ScaleToSpeckle(connector.Radius);
      }
      catch { }
      foreach (var reference in connector.AllRefs.Cast<Connector>())
      {
        if (connector.IsConnectedTo(reference))
        {
          speckleMEPConnector.connectedConnectorIds.Add(reference.GetUniqueApplicationId());
        }
      }
      return speckleMEPConnector;
    }
  }
}
