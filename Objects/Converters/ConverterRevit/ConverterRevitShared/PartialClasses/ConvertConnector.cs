using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit;

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

    if (connector.Domain is Domain.DomainHvac or Domain.DomainPiping or Domain.DomainCableTrayConduit)
    {
      speckleMEPConnector.angle = connector.Angle;
    }

    if (connector.Shape is ConnectorProfileType.Rectangular)
    {
      speckleMEPConnector.height = ScaleToSpeckle(connector.Height);
      speckleMEPConnector.width = ScaleToSpeckle(connector.Width);
    }
    else if (connector.Shape is ConnectorProfileType.Round)
    {
      speckleMEPConnector.radius = ScaleToSpeckle(connector.Radius);
    }

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
