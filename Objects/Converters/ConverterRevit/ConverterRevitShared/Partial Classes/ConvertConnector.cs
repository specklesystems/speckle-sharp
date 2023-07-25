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
      var speckleMEPConnector = new RevitMEPConnector();
      speckleMEPConnector.applicationId = connector.GetUniqueApplicationId();
      speckleMEPConnector.Origin = PointToSpeckle(connector.Origin, Doc);
      foreach (var reference in connector.AllRefs.Cast<Connector>())
      {
        if (connector.IsConnectedTo(reference))
        {
          speckleMEPConnector.ConnectedConnectorIds.Add(reference.GetUniqueApplicationId());
        }
      }
      return speckleMEPConnector;
    }
  }
}
