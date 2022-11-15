using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Network = Objects.BuiltElements.Network;
using NetworkElement = Objects.BuiltElements.NetworkElement;
using NetworkLink = Objects.BuiltElements.NetworkLink;
using RevitNetworkElement = Objects.BuiltElements.Revit.RevitNetworkElement;
using RevitNetworkLink = Objects.BuiltElements.Revit.RevitNetworkLink;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject NetworkToNative(Network speckleNetwork)
    {
      var appObj = new ApplicationObject(speckleNetwork.id, speckleNetwork.speckle_type) { applicationId = speckleNetwork.applicationId };

      speckleNetwork.elements.ForEach(e => e.network = speckleNetwork);
      speckleNetwork.links.ForEach(l => l.network = speckleNetwork);

      // convert all the MEP trades and family instances except fittings

      var convertedElements = new Dictionary<string, Element>();
      var elements = speckleNetwork.elements.Cast<RevitNetworkElement>().ToList();
      var notConnectorBasedCreationElements = elements.Where(e => !e.isConnectorBased).ToArray();
      foreach (var networkElement in notConnectorBasedCreationElements)
      {
        var element = networkElement.element;
        if (CanConvertToNative(element))
        {
          var convAppObj = ConvertToNative(element) as ApplicationObject;
          foreach (var obj in convAppObj.Converted)
          {
            var nativeElement = obj as Element;
            appObj.Update(status: ApplicationObject.State.Created, createdId: nativeElement.UniqueId, convertedItem: nativeElement);
          }
          convertedElements.Add(networkElement.applicationId, convAppObj.Converted.First() as Element);
        }
        else
        {
          appObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Receiving this object type is not supported in Revit");
        }
      }

      // convert connector based creation elements, We use different way to create curve fittings such as elbow,
      // transition, tee, union, cross then other family instances since creation are based on connectors, two,
      // three or four depends on type of fitting.

      var connectorBasedCreationElements = elements.Where(e => e.isConnectorBased).ToArray();
      var convertedMEPCurves = convertedElements.Where(e => e.Value is MEPCurve).ToArray();
      foreach (var networkElement in connectorBasedCreationElements)
      {
        if (!GetElementType(networkElement.element, appObj, out FamilySymbol familySymbol))
        {
          appObj.Update(status: ApplicationObject.State.Failed);
          continue;
        }
        
        DB.FamilyInstance familyInstance = null;

        var tempCurves = new Dictionary<int, MEPCurve>();

        foreach (var link in networkElement.links)
        {
          if (link is RevitNetworkLink revitLink && !revitLink.needsPlaceholders)
          {
            var curve = CreateCurve(revitLink);
            tempCurves.Add(revitLink.fittingIndex, curve);
          }
        }

        var connections = networkElement.links.Cast<RevitNetworkLink>().ToDictionary(
          l => l,
          l => l.elements
          .Cast<RevitNetworkElement>()
          .FirstOrDefault(e => e.applicationId != networkElement.applicationId
          && e.isCurveBased));

        var connection1 = connections.FirstOrDefault(c => c.Key.fittingIndex == 1);
        var connection2 = connections.FirstOrDefault(c => c.Key.fittingIndex == 2);
        var connection3 = connections.FirstOrDefault(c => c.Key.fittingIndex == 3);
        var connection4 = connections.FirstOrDefault(c => c.Key.fittingIndex == 4);

        var element1 = connection1.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection1.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 1).Value;
        var element2 = connection2.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection2.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 2).Value;
        var element3 = connection3.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection3.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 3).Value;
        var element4 = connection4.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection4.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 4).Value;

        var connector1 = element1 != null ? GetConnectorByPoint(element1, PointToNative(connection1.Key.origin)) : null;
        var connector2 = element2 != null ? GetConnectorByPoint(element2, PointToNative(connection2.Key.origin)) : null;
        var connector3 = element3 != null ? GetConnectorByPoint(element3, PointToNative(connection3.Key.origin)) : null;
        var connector4 = element4 != null ? GetConnectorByPoint(element4, PointToNative(connection4.Key.origin)) : null;

        var partType = networkElement.element["partType"] as string ?? "Unknown";
        if (partType.Contains("Elbow") && connector1 != null && connector2 != null)
          familyInstance = Doc.Create.NewElbowFitting(connector1, connector2);
        else if (partType.Contains("Transition") && connector1 != null && connector2 != null)
          familyInstance = Doc.Create.NewTransitionFitting(connector1, connector2);
        else if (partType.Contains("Union") && connector1 != null && connector2 != null)
          familyInstance = Doc.Create.NewUnionFitting(connector1, connector2);
        else if (partType.Contains("Tee") && connector1 != null && connector2 != null && connector3 != null)
          familyInstance = Doc.Create.NewTeeFitting(connector1, connector2, connector3);
        else if (partType.Contains("Cross") && connector1 != null && connector2 != null && connector3 != null && connector4 != null)
          familyInstance = Doc.Create.NewCrossFitting(connector1, connector2, connector3, connector4);
        else
        {
          var convAppObj = ConvertToNative(networkElement.element) as ApplicationObject;
          foreach (var obj in convAppObj.Converted)
          {
            var nativeElement = obj as Element;
            appObj.Update(status: ApplicationObject.State.Created, createdId: nativeElement.UniqueId, convertedItem: nativeElement);
          }
        }

        if (familyInstance != null)
        {
          convertedElements.Add(networkElement.applicationId, familyInstance);
          familyInstance?.ChangeTypeId(familySymbol.Id);
          Doc.Delete(tempCurves.Select(c => c.Value.Id).ToList());

          appObj.Update(status: ApplicationObject.State.Created, createdId: familyInstance.UniqueId, convertedItem: familyInstance);
        }
        else
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: "Family instance was null");
        }
      }

      // check if all the elements are connected, some connectors may be unconnected
      // due using of temp curves and until all the ones are created no way to connect
      // them between each other

      var links = speckleNetwork.links.Cast<RevitNetworkLink>().ToArray();
      foreach (var link in links)
      {
        if (link.isConnected && link.elements.Count == 2)
        {
          var firstElement = convertedElements.FirstOrDefault(e => e.Key == link.elements[0].applicationId).Value;
          var secondElement = convertedElements.FirstOrDefault(e => e.Key == link.elements[1].applicationId).Value;
          var origin = new XYZ(link.origin.x, link.origin.y, link.origin.z);
          var firstConnector = GetConnectorByPoint(firstElement, origin);
          var secondConnector = GetConnectorByPoint(secondElement, origin);
          if (firstConnector != null
            && secondConnector != null
            && !firstConnector.IsConnected
            && !secondConnector.IsConnected)
          {
            firstConnector.ConnectTo(secondConnector);
          }
        }
      }

      return appObj;
    }

    public Network NetworkToSpeckle(Element mepElement, out List<string> notes)
    {
      notes = new List<string>();
      Network speckleNetwork = new Network() { name = mepElement.Name, elements = new List<NetworkElement>(), links = new List<NetworkLink>() };

      GetNetworkElements(speckleNetwork, mepElement, out List<string> connectedNotes);
      if (connectedNotes.Any()) notes.AddRange(connectedNotes);
      return speckleNetwork;
    }

    private Connector GetConnectorByPoint(Element element, XYZ point)
    {
      switch (element)
      {
        case MEPCurve o:
          return o.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point, 0.00001));
        case DB.FamilyInstance o:
          return o.MEPModel?.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point, 0.00001));
        default:
          return null;
      }
    }

    private static MEPCurveType GetDefaultMEPCurveType(Document doc, Domain domain, ConnectorProfileType shape)
    {
      switch (domain)
      {
        case Domain.DomainHvac:
          return GetDefaultMEPCurveType(doc, typeof(DuctType), shape);
        case Domain.DomainPiping:
          return GetDefaultMEPCurveType(doc, typeof(PipeType), shape);
        case Domain.DomainElectrical:
          return GetDefaultMEPCurveType(doc, typeof(ConduitType), shape);
        case Domain.DomainCableTrayConduit:
          return GetDefaultMEPCurveType(doc, typeof(CableTrayType), shape);
        default:
          throw new Exception();
      }
    }

    private static MEPCurveType GetDefaultMEPCurveType(Document doc, Type type, ConnectorProfileType shape)
    {
      return new FilteredElementCollector(doc)
          .WhereElementIsElementType()
          .OfClass(type)
          .FirstOrDefault(t => t is MEPCurveType type && type.Shape == shape) as MEPCurveType;
    }

    private MEPCurve CreateCurve(RevitNetworkLink link)
    {
      var direction = new XYZ(link.direction.x, link.direction.y, link.direction.z);
      var start = new XYZ(link.origin.x, link.origin.y, link.origin.z);
      var end = start.Add(direction.Multiply(2));
      var domain = SpeckleToRevitDomain(link.domain);
      var shape = SpeckleToRevitShape(link.shape);
      var curveType = GetDefaultMEPCurveType(Doc, domain, shape);
      var sfi = link.elements.FirstOrDefault(e => e.element is BuiltElements.Revit.FamilyInstance)?.element as BuiltElements.Revit.FamilyInstance;
      Level level = ConvertLevelToRevit(sfi.level, out ApplicationObject.State state);
      var systemTypes = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(MEPSystemType)).ToElements().Cast<ElementType>();
      MEPCurve curve = null;
      switch (domain)
      {
        case Domain.DomainHvac:
          if (!(systemTypes.Where(st => st is MechanicalSystemType).FirstOrDefault(x => x.Name == link.systemType) is MechanicalSystemType mechanicalSystemType))
          {
            mechanicalSystemType = systemTypes.Where(st => st is MechanicalSystemType).First() as MechanicalSystemType;
          }
          curve = Duct.Create(Doc, mechanicalSystemType.Id, curveType.Id, level.Id, start, end);
          if (curveType.Shape == ConnectorProfileType.Round)
          {
            curve.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).Set(link.diameter);
          }
          else
          {
            curve.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).Set(link.width);
            curve.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).Set(link.height);
          }
          break;
        case Domain.DomainPiping:
          if (!(systemTypes.Where(st => st is PipingSystemType).FirstOrDefault(x => x.Name == link.systemType) is PipingSystemType pipingSystemType))
          {
            pipingSystemType = systemTypes.Where(st => st is PipingSystemType).First() as PipingSystemType;
          }
          curve = Pipe.Create(Doc, pipingSystemType.Id, curveType.Id, level.Id, start, end);
          curve.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(link.diameter);
          break;
        case Domain.DomainElectrical:
          curve = Conduit.Create(Doc, curveType.Id, start, end, level.Id);
          curve.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).Set(link.diameter);
          break;
        case Domain.DomainCableTrayConduit:
          curve = CableTray.Create(Doc, curveType.Id, start, end, level.Id);
          curve.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).Set(link.width);
          curve.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).Set(link.height);
          break;
      }
      return curve;
    }
  }
}
