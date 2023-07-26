using System;
using System.Collections.Generic;
using System.Text;
using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.FamilyInstance FittingToNative(RevitMEPFamilyInstance speckleRevitFitting, PartType partType)
    {
      List<Connector> connectors = new();

      foreach (var speckleRevitConnector in speckleRevitFitting.Connectors)
      {
        foreach (var existingConnector in GetRevitConnectorsThatConnectToSpeckleConnector(
          speckleRevitConnector, 
          receivedObjectsCache))
        {
          if (existingConnector != null) connectors.Add(existingConnector);
        }
      }

      DB.FamilyInstance familyInstance;
      switch (partType)
      {
        case PartType.Elbow:
          if (connectors.Count != 2) throw new ConversionNotReadyException("All connectors must be converted before fitting");
          familyInstance = Doc.Create.NewElbowFitting(connectors[0], connectors[1]);
          break;
        case PartType.Transition:
          if (connectors.Count != 2) throw new ConversionNotReadyException("All connectors must be converted before fitting");
          familyInstance = Doc.Create.NewTransitionFitting(connectors[0], connectors[1]);
          break;
        case PartType.Union:
          if (connectors.Count != 2) throw new ConversionNotReadyException("All connectors must be converted before fitting");
          familyInstance = Doc.Create.NewUnionFitting(connectors[0], connectors[1]);
          break;
        case PartType.Tee:
          if (connectors.Count != 3) throw new ConversionNotReadyException("All connectors must be converted before fitting");
          familyInstance = Doc.Create.NewTeeFitting(connectors[0], connectors[1], connectors[2]);
          break;
        case PartType.Cross:
          if (connectors.Count != 4) throw new ConversionNotReadyException("All connectors must be converted before fitting");
          familyInstance = Doc.Create.NewCrossFitting(connectors[0], connectors[1], connectors[2], connectors[3]);
          break;
        default:
          familyInstance = null;
          break;
      }

      var familySymbol = GetElementType<FamilySymbol>(speckleRevitFitting, new ApplicationObject(null, null), out bool isExactMatch);

      if (isExactMatch && familyInstance?.Symbol.Id.IntegerValue != familySymbol.Id.IntegerValue)
      {
        familyInstance?.ChangeTypeId(familySymbol.Id);
      }

      return familyInstance;
    }
  }
}
