using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public RevitMEPConnector ConnectorToSpeckle(Connector connector)
  {
    var speckleMepConnector = new RevitMEPConnector
    {
      applicationId = connector.GetUniqueApplicationId(),
      shape = connector.Shape.ToString(),
      systemName = (connector.MEPSystem?.Name ?? connector.Owner.Category?.Name) ?? string.Empty
    };

    try
    {
      speckleMepConnector.origin = PointToSpeckle(connector.Origin, Doc);
    }
    catch (Autodesk.Revit.Exceptions.InvalidOperationException)
    {
      // ignore this exception if there is no origin, we cant report it.
      // and yet there is no discovery of a Physical connector type.
    }

    if (connector.Domain is Domain.DomainHvac or Domain.DomainPiping or Domain.DomainCableTrayConduit)
    {
      speckleMepConnector.angle = connector.Angle;
    }

    if (connector.Shape == ConnectorProfileType.Rectangular)
    {
      speckleMepConnector.height = ScaleToSpeckle(connector.Height);
      speckleMepConnector.width = ScaleToSpeckle(connector.Width);
    }
    else if (connector.Shape == ConnectorProfileType.Round)
    {
      speckleMepConnector.radius = ScaleToSpeckle(connector.Radius);
    }

    foreach (var reference in connector.AllRefs.Cast<Connector>())
    {
      if (connector.IsConnectedTo(reference))
      {
        speckleMepConnector.connectedConnectorIds.Add(reference.GetUniqueApplicationId());
      }
    }

    return speckleMepConnector;
  }
}
