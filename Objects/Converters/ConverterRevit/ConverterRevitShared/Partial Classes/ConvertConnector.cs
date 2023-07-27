using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        angle = connector.Angle,
        applicationId = connector.GetUniqueApplicationId(),
        origin = PointToSpeckle(connector.Origin, Doc),
        shape = connector.Shape.ToString(),
        systemName = connector.MEPSystem?.Name ?? connector.Owner.Category?.Name,
      };
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
