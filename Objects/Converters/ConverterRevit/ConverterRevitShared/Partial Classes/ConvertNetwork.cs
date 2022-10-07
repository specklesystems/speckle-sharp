using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Objects.Organization.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Network = Objects.Organization.Network;

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
      var notConnectorBasedCreationElements = elements.Where(e => !e.connectorBasedCreation).ToArray();
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

      var connectorBasedCreationElements = elements.Where(e => e.connectorBasedCreation).ToArray();
      var convertedMEPCurves = convertedElements.Where(e => e.Value is MEPCurve).ToArray();
      foreach (var networkElement in connectorBasedCreationElements)
      {
        //FamilySymbol familySymbol = GetElementType<FamilySymbol>(networkElement.element);
        if (!GetElementType(networkElement.element, appObj, out FamilySymbol familySymbol))
        {
          appObj.Update(status: ApplicationObject.State.Failed);
          continue;
        }
        
        FamilyInstance familyInstance = null;

        var tempCurves = new Dictionary<int, MEPCurve>();

        foreach (var link in networkElement.links)
        {
          if (link is RevitNetworkLink revitLink && !revitLink.connectedToCurve)
          {
            var curve = CreateCurve(revitLink);
            tempCurves.Add(revitLink.connectionIndex, curve);
          }
        }

        var connections = networkElement.links.Cast<RevitNetworkLink>().ToDictionary(
          l => l,
          l => l.elements
          .Cast<RevitNetworkElement>()
          .FirstOrDefault(e => e.applicationId != networkElement.applicationId
          && e.isCurve));

        var firstConnection = connections.FirstOrDefault(c => c.Key.connectionIndex == 1);
        var secondConnection = connections.FirstOrDefault(c => c.Key.connectionIndex == 2);
        var thirdConnection = connections.FirstOrDefault(c => c.Key.connectionIndex == 3);
        var fourthConnection = connections.FirstOrDefault(c => c.Key.connectionIndex == 4);

        var firstElement = firstConnection.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == firstConnection.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 1).Value;
        var secondElement = secondConnection.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == secondConnection.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 2).Value;
        var thirdElement = thirdConnection.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == thirdConnection.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 3).Value;
        var fourthElement = fourthConnection.Value != null ? convertedMEPCurves.FirstOrDefault(e => e.Key == fourthConnection.Value.applicationId).Value : tempCurves.FirstOrDefault(t => t.Key == 4).Value;

        var firstConnector = firstElement != null ? GetConnectorByPoint(firstElement, SpeckleToRevitPoint(firstConnection.Key.origin)) : null;
        var secondConnector = secondElement != null ? GetConnectorByPoint(secondElement, SpeckleToRevitPoint(secondConnection.Key.origin)) : null;
        var thirdConnector = thirdElement != null ? GetConnectorByPoint(thirdElement, SpeckleToRevitPoint(thirdConnection.Key.origin)) : null;
        var fourthConnector = fourthElement != null ? GetConnectorByPoint(fourthElement, SpeckleToRevitPoint(fourthConnection.Key.origin)) : null;

        if (networkElement.fittingType == FittingType.Elbow && firstConnector != null && secondConnector != null)
        {
          familyInstance = Doc.Create.NewElbowFitting(firstConnector, secondConnector);
        }
        else if (networkElement.fittingType == FittingType.Transition && firstConnector != null && secondConnector != null)
        {
          familyInstance = Doc.Create.NewTransitionFitting(firstConnector, secondConnector);
        }
        else if (networkElement.fittingType == FittingType.Union && firstConnector != null && secondConnector != null)
        {
          familyInstance = Doc.Create.NewUnionFitting(firstConnector, secondConnector);
        }
        else if (networkElement.fittingType == FittingType.Tee && firstConnector != null && secondConnector != null && thirdConnector != null)
        {
          familyInstance = Doc.Create.NewTeeFitting(firstConnector, secondConnector, thirdConnector);
        }
        else if (networkElement.fittingType == FittingType.Cross && firstConnector != null && secondConnector != null && thirdConnector != null && fourthConnector != null)
        {
          familyInstance = Doc.Create.NewCrossFitting(firstConnector, secondConnector, thirdConnector, fourthConnector);
        }
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
        if (link.connected && link.elements.Count == 2)
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
      Network speckleNetwork = new Network() { name = mepElement.Name, elements = new List<Organization.NetworkElement>(), links = new List<Organization.NetworkLink>() };

      GetNetworkElements(speckleNetwork, mepElement, out List<string> connectedNotes);
      if (connectedNotes.Any()) notes.AddRange(connectedNotes);
      return speckleNetwork;
    }

    private Connector GetConnectorByPoint(Element element, XYZ point)
    {
      if (element is MEPCurve curve)
      {
        return curve.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point, 0.00001));
      }
      else if (element is DB.FamilyInstance familyInstance)
      {
        return familyInstance.MEPModel?.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point, 0.00001));
      }
      return null;
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
          if (!(systemTypes.Where(st => st is MechanicalSystemType).FirstOrDefault(x => x.Name == link.type) is MechanicalSystemType mechanicalSystemType))
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
          if (!(systemTypes.Where(st => st is PipingSystemType).FirstOrDefault(x => x.Name == link.type) is PipingSystemType pipingSystemType))
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

    private static XYZ SpeckleToRevitPoint(Geometry.Point point)
    {
      return new XYZ(point.x, point.y, point.z);
    }

    private static Geometry.Point RevitToSpecklePoint(XYZ point)
    {
      return new Geometry.Point(point.X, point.Y, point.Z, Speckle.Core.Kits.Units.Feet);
    }
  }
}
