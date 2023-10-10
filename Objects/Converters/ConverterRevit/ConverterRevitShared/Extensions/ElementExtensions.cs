#nullable enable
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
      return element.GetConnectorManager()?.Connectors?.Cast<Connector>() ?? empty;
    }

    public static ConnectorManager? GetConnectorManager(this Element element)
    {
      return element switch
      {
        MEPCurve o => o.ConnectorManager,
        FamilyInstance o => o.MEPModel?.ConnectorManager,
        _ => null,
      };
    }

    public static bool IsMEPElement(this Element element)
    {
      return element.GetConnectorManager() != null;
    }
  }
}
