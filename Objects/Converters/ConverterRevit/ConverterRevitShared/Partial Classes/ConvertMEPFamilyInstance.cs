using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using System.Linq;
using Objects.Organization;
using Autodesk.Revit.DB.Mechanical;
using ConverterRevitShared.Extensions;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public RevitMEPFamilyInstance MEPFamilyInstanceToSpeckle(DB.FamilyInstance familyInstance)
    {
      var speckleFi = new RevitMEPFamilyInstance();

      foreach (var connector in familyInstance.MEPModel?.ConnectorManager?.Connectors?.Cast<Connector>())
      {
        speckleFi.Connectors.Add(ConnectorToSpeckle(connector));
      }

      _ = RevitInstanceToSpeckle(familyInstance, out _, null, existingInstance: speckleFi);
      return speckleFi;
    }

    public DB.FamilyInstance MEPFamilyInstanceToNative(RevitMEPFamilyInstance speckleFi)
    {
      var appObj = RevitInstanceToNative(speckleFi);
      var revitFi = (DB.FamilyInstance)appObj.Converted.First();

      // hack with magic string... not great
      if (speckleFi["graph"] is Graph graph)
      {
        CreateSystemConnections(speckleFi.Connectors, revitFi, graph, receivedObjectsCache);
      }

      return revitFi;
    }
  }
}
