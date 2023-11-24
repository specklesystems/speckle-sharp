using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Logging;
using System;
using ConverterRevitShared.Extensions;
using System.Linq;

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

      var connectorInfo = ValidateConnectorsAndPopulateList(speckleRevitFitting);

      var familyInstance = TryCreateFitting(partType, docObj, connectorInfo)
        ?? throw new SpeckleException($"{nameof(FittingToNative)} yeilded a null familyInstance");

      var familySymbol = GetElementType<FamilySymbol>(speckleRevitFitting, new ApplicationObject(null, null), out bool isExactMatch);

      if (isExactMatch && familyInstance.Symbol.Id.IntegerValue != familySymbol.Id.IntegerValue)
      {
        familyInstance.ChangeTypeId(familySymbol.Id);
      }

      appObj.Update(status: ApplicationObject.State.Created, createdId: familyInstance.UniqueId, convertedItem: familyInstance);

      return familyInstance;
    }

    private DB.FamilyInstance TryCreateFitting(PartType partType, Element docObj, List<(Element, int)> connectorInfo)
    {
      switch (partType)
      {
        case PartType.Elbow:
          if (connectorInfo.Count != 2)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Elbow)} must have 2 connectors, not {connectorInfo.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewElbowFitting(GetConnector(connectorInfo[0]), GetConnector(connectorInfo[1]));
        case PartType.Transition:
          if (connectorInfo.Count != 2)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Transition)} must have 2 connectors, not {connectorInfo.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          var revitFitting = TryInSubtransaction(
            () => Doc.Create.NewTransitionFitting(GetConnector(connectorInfo[0]), GetConnector(connectorInfo[1])),
            ex => { }
          );
          revitFitting ??= TryInSubtransaction(
            () => Doc.Create.NewTransitionFitting(GetConnector(connectorInfo[1]), GetConnector(connectorInfo[0])),
            ex => throw ex
          );
          return revitFitting;
        case PartType.Union:
          if (connectorInfo.Count != 2)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Union)} must have 2 connectors, not {connectorInfo.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewUnionFitting(GetConnector(connectorInfo[0]), GetConnector(connectorInfo[1]));
        case PartType.Tee:
          if (connectorInfo.Count != 3)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Tee)} must have 3 connectors, not {connectorInfo.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewTeeFitting(GetConnector(connectorInfo[0]), GetConnector(connectorInfo[1]), GetConnector(connectorInfo[2]));
        case PartType.Cross:
          if (connectorInfo.Count != 4)
          {
            throw new SpeckleException($"A fitting with a partType of {nameof(PartType.Cross)} must have 4 connectors, not {connectorInfo.Count}");
          }
          if (ReceiveMode == ReceiveMode.Update && docObj != null)
          {
            Doc.Delete(docObj.Id);
          }
          return Doc.Create.NewCrossFitting(GetConnector(connectorInfo[0]), GetConnector(connectorInfo[1]), GetConnector(connectorInfo[2]), GetConnector(connectorInfo[3]));
        default:
          throw new SpeckleException($"Method named {nameof(FittingToNative)} was not expecting an element with a partType of {partType}");
      }
    }

    /// <summary>
    /// Attempting to add a fitting between two connectors and rolling back a subtransaction will result in the
    /// connector element references becomeing invalid. Therefore, instead of passing around the connector objects,
    /// we're keeping track of the owner element and the connector id so we can retrieve the connector
    /// connector id
    /// </summary>
    /// <param name="connectorInfo"></param>
    /// <returns></returns>
    private Connector GetConnector((Element, int) connectorInfo) => connectorInfo.Item1
      .GetConnectorSet()
      .First(c => c.Id == connectorInfo.Item2);

    private List<(Element, int)> ValidateConnectorsAndPopulateList(RevitMEPFamilyInstance speckleRevitFitting)
    {
      List<(Element, int)> connectors = new();
      foreach (var speckleRevitConnector in speckleRevitFitting.Connectors)
      {
        var con = FindNativeConnectorForSpeckleRevitConnector(speckleRevitConnector);
        connectors.Add(con);
      }
      return connectors;
    }
    
    private (Element, int) FindNativeConnectorForSpeckleRevitConnector(RevitMEPConnector speckleRevitConnector)
    {
      List<Exception> exceptions = new();
      foreach (var (elementAppId, element, existingConnector) in GetRevitConnectorsThatConnectToSpeckleConnector(
          speckleRevitConnector,
          receivedObjectsCache))
      {
        if (existingConnector != null)
        {
          // we only want one native connector per speckleRevitConnector so return if we find a match
          return (element, existingConnector.Id);
        }
        else if (element != null)
        {
          // if the element is not null but the connector is, then the correct connector on the element could not 
          // be found by trying to compare locations of all the connectors on the element
          exceptions.Add(new SpeckleException("Fitting found native element to connect to but could not find \"connector\" subelement which is needed for connection in Revit"));
        }
        else if (string.IsNullOrEmpty(elementAppId))
        {
          exceptions.Add(new SpeckleException("A connector has a reference to a null applicationId"));
        }
        else if (ContextObjects.ContainsKey(elementAppId))
        {
          // here a native element could not be found. Hopefully it is yet to be converted and we can 
          // try to convert the fitting later
          throw new ConversionNotReadyException("All connectors must be converted before fitting");
        }
        else
        {
          // TODO: the fitting doesn't exist in the incoming commit. Maybe we can add a placeholder?
          exceptions.Add(new SpeckleException("The element that the fitting connects to is not in the incoming Commit object"));
        }
      }
      throw new AggregateException(exceptions);
    }
  }
}
