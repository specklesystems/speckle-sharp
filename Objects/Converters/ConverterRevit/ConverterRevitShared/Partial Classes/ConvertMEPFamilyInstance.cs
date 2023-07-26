using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using System.Linq;
using Objects.Organization;
using System;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<PartType> FittingPartTypes { get; } = new List<PartType>()
    { 
      PartType.Elbow, 
      PartType.Tee, 
      PartType.Cross, 
      PartType.Transition, 
      PartType.Union 
    };

    public RevitMEPFamilyInstance MEPFamilyInstanceToSpeckle(DB.FamilyInstance familyInstance, RevitMEPFamilyInstance existingSpeckleObject = null)
    {
      var speckleFi = existingSpeckleObject ?? new RevitMEPFamilyInstance();

      var partType = GetParamValue<PartType>(familyInstance.Symbol.Family, BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
      speckleFi.RevitPartType = partType.ToString();

      foreach (var connector in familyInstance.MEPModel?.ConnectorManager?.Connectors?.Cast<Connector>())
      {
        speckleFi.Connectors.Add(ConnectorToSpeckle(connector));
      }

      _ = RevitInstanceToSpeckle(familyInstance, out _, null, existingInstance: speckleFi);
      return speckleFi;
    }

    public DB.FamilyInstance MEPFamilyInstanceToNative(RevitMEPFamilyInstance speckleFi)
    {
      if (Enum.TryParse<PartType>(speckleFi.RevitPartType, out var partType))
      {
        if (FittingPartTypes.Contains(partType))
        {
          return FittingToNative(speckleFi, partType);
        }
      }
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
