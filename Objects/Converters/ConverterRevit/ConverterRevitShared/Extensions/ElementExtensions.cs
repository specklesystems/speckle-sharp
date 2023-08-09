using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace ConverterRevitShared.Extensions
{
  public static class ElementExtensions
  {
    public static IEnumerable<Connector> GetConnectorSet(this Element element)
    {
      var empty = Enumerable.Empty<Connector>();
      return element switch
      {
        CableTray o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        Conduit o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        Duct o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        FamilyInstance o => o.MEPModel?.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        FlexDuct o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        FlexPipe o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        Pipe o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        Wire o => o.ConnectorManager?.Connectors?.Cast<Connector>() ?? empty,
        _ => Enumerable.Empty<Connector>(),
      };
    }
  }
}
