using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Logging;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.FamilyInstance FittingToNative(RevitMEPFamilyInstance speckleRevitFitting, PartType partType, ApplicationObject appObj)
    {
      var docObj = GetExistingElementByApplicationId(speckleRevitFitting.applicationId);
      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return null;

      List<Connector> connectors = new();
      ValidateConnectorsAndPopulateList(speckleRevitFitting, connectors);

      var familyInstance = TryCreateFitting(partType, docObj, connectors)
        ?? throw new SpeckleException($"{nameof(FittingToNative)} yeilded a null familyInstance");

      var familySymbol = GetElementType<FamilySymbol>(speckleRevitFitting, new ApplicationObject(null, null), out bool isExactMatch);

      if (isExactMatch && familyInstance.Symbol.Id.IntegerValue != familySymbol.Id.IntegerValue)
      {
        familyInstance.ChangeTypeId(familySymbol.Id);
      }

      appObj.Update(status: ApplicationObject.State.Created, createdId: familyInstance.UniqueId, convertedItem: familyInstance);

      return familyInstance;
    }

    private DB.FamilyInstance TryCreateFitting(PartType partType, Element docObj, List<Connector> connectors)
    {
      switch (partType)
      {
        case PartType.Elbow:
          if (connectors.Count != 2)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Elbow)} must have 2 connectors, not {connectors.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewElbowFitting(connectors[0], connectors[1]);
        case PartType.Transition:
          if (connectors.Count != 2)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Transition)} must have 2 connectors, not {connectors.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewTransitionFitting(connectors[0], connectors[1]);
        case PartType.Union:
          if (connectors.Count != 2)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Union)} must have 2 connectors, not {connectors.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewUnionFitting(connectors[0], connectors[1]);
        case PartType.Tee:
          if (connectors.Count != 3)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Tee)} must have 3 connectors, not {connectors.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewTeeFitting(connectors[0], connectors[1], connectors[2]);
        case PartType.Cross:
          if (connectors.Count != 4)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Cross)} must have 4 connectors, not {connectors.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewCrossFitting(connectors[0], connectors[1], connectors[2], connectors[3]);
        default:
          throw new SpeckleException($"Method named {nameof(FittingToNative)} was not expecting an element with a partType of {partType}");
      }
    }

    private void ValidateConnectorsAndPopulateList(
      RevitMEPFamilyInstance speckleRevitFitting, 
      List<Connector> connectors
    )
    {
      foreach (var speckleRevitConnector in speckleRevitFitting.Connectors)
      {
        foreach (var (elementAppId, element, existingConnector) in GetRevitConnectorsThatConnectToSpeckleConnector(
          speckleRevitConnector,
          receivedObjectsCache))
        {
          if (existingConnector != null)
          {
            connectors.Add(existingConnector);
          }
          else if (element != null)
          {
            // if the element is not null but the connector is, then the correct connector on the element could not 
            // be found by trying to compare locations of all the connectors on the element
            throw new SpeckleException("Fitting found native element to connect to but could not find \"connector\" subelement which is needed for connection in Revit");
          }
          else if (string.IsNullOrEmpty(elementAppId))
          {
            throw new SpeckleException("A connector has a reference to a null applicationId");
          }
          else if (ContextObjects.ContainsKey(elementAppId))
          {
            // here a native element could not be found. Hopefully it is yet to be converted and we can 
            // try to convert the fitting later
            throw new ConversionNotReadyException("All connectors must be converted before fitting");
          }
          else
          {
            throw new SpeckleException("Something went wrong when trying to convert fitting.");
          }
        }
      }
    }
  }
}
