using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using ConverterRevitShared.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Network = Objects.BuiltElements.Network;
using NetworkElement = Objects.BuiltElements.NetworkElement;
using NetworkLink = Objects.BuiltElements.NetworkLink;
using RevitNetworkElement = Objects.BuiltElements.Revit.RevitNetworkElement;
using RevitNetworkLink = Objects.BuiltElements.Revit.RevitNetworkLink;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject NetworkToNative(Network speckleNetwork)
  {
    var appObj = new ApplicationObject(speckleNetwork.id, speckleNetwork.speckle_type)
    {
      applicationId = speckleNetwork.applicationId
    };

    speckleNetwork.elements.ForEach(e => e.network = speckleNetwork);
    speckleNetwork.links.ForEach(l => l.network = speckleNetwork);

    // convert all the MEP trades and family instances except fittings

    var convertedElements = new Dictionary<string, Element>();
    var elements = speckleNetwork.elements.Cast<RevitNetworkElement>().ToList();
    var notConnectorBasedCreationElements = elements.Where(e => !e.isConnectorBased).ToArray();
    foreach (var networkElement in notConnectorBasedCreationElements)
    {
      var element = networkElement.elements;
      if (CanConvertToNative(element))
      {
        var convAppObj = ConvertToNative(element) as ApplicationObject;
        foreach (var obj in convAppObj.Converted)
        {
          var nativeElement = obj as Element;
          appObj.Update(
            status: ApplicationObject.State.Created,
            createdId: nativeElement.UniqueId,
            convertedItem: nativeElement
          );
        }
        convertedElements.Add(networkElement.applicationId, convAppObj.Converted.First() as Element);
      }
      else
      {
        appObj.Update(
          status: ApplicationObject.State.Skipped,
          logItem: $"Receiving this object type is not supported in Revit"
        );
      }
    }

    // convert connector based creation elements, We use different way to create curve fittings such as elbow,
    // transition, tee, union, cross then other family instances since creation are based on connectors, two,
    // three or four depends on type of fitting.

    var connectorBasedCreationElements = elements.Where(e => e.isConnectorBased).ToArray();
    var convertedMEPCurves = convertedElements.Where(e => e.Value is MEPCurve).ToArray();
    foreach (var networkElement in connectorBasedCreationElements)
    {
      var familySymbol = GetElementType<FamilySymbol>(networkElement.elements, appObj, out bool _);
      if (familySymbol == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        continue;
      }

      DB.FamilyInstance familyInstance = null;

      var tempCurves = new Dictionary<int, MEPCurve>();

      foreach (var link in networkElement.links)
      {
        if (link is RevitNetworkLink revitLink && revitLink.needsPlaceholders)
        {
          var curve = CreateCurve(revitLink);
          tempCurves.Add(revitLink.fittingIndex, curve);
        }
      }

      var connections = networkElement.links
        .Cast<RevitNetworkLink>()
        .ToDictionary(
          l => l,
          l =>
            l.elements
              .Cast<RevitNetworkElement>()
              .FirstOrDefault(e => e.applicationId != networkElement.applicationId && e.isCurveBased)
        );

      var connection1 = connections.FirstOrDefault(c => c.Key.fittingIndex == 1);
      var connection2 = connections.FirstOrDefault(c => c.Key.fittingIndex == 2);
      var connection3 = connections.FirstOrDefault(c => c.Key.fittingIndex == 3);
      var connection4 = connections.FirstOrDefault(c => c.Key.fittingIndex == 4);

      var element1 =
        connection1.Value != null
          ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection1.Value.applicationId).Value
          : tempCurves.FirstOrDefault(t => t.Key == 1).Value;
      var element2 =
        connection2.Value != null
          ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection2.Value.applicationId).Value
          : tempCurves.FirstOrDefault(t => t.Key == 2).Value;
      var element3 =
        connection3.Value != null
          ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection3.Value.applicationId).Value
          : tempCurves.FirstOrDefault(t => t.Key == 3).Value;
      var element4 =
        connection4.Value != null
          ? convertedMEPCurves.FirstOrDefault(e => e.Key == connection4.Value.applicationId).Value
          : tempCurves.FirstOrDefault(t => t.Key == 4).Value;

      var connector1 = element1 != null ? GetConnectorByPoint(element1, PointToNative(connection1.Key.origin)) : null;
      var connector2 = element2 != null ? GetConnectorByPoint(element2, PointToNative(connection2.Key.origin)) : null;
      var connector3 = element3 != null ? GetConnectorByPoint(element3, PointToNative(connection3.Key.origin)) : null;
      var connector4 = element4 != null ? GetConnectorByPoint(element4, PointToNative(connection4.Key.origin)) : null;

      var partType = networkElement.elements["partType"] as string ?? "Unknown";
      if (partType.Contains("Elbow") && connector1 != null && connector2 != null)
      {
        familyInstance = Doc.Create.NewElbowFitting(connector1, connector2);
      }
      else if (partType.Contains("Transition") && connector1 != null && connector2 != null)
      {
        familyInstance = Doc.Create.NewTransitionFitting(connector1, connector2);
      }
      else if (partType.Contains("Union") && connector1 != null && connector2 != null)
      {
        familyInstance = Doc.Create.NewUnionFitting(connector1, connector2);
      }
      else if (partType.Contains("Tee") && connector1 != null && connector2 != null && connector3 != null)
      {
        familyInstance = Doc.Create.NewTeeFitting(connector1, connector2, connector3);
      }
      else if (
        partType.Contains("Cross")
        && connector1 != null
        && connector2 != null
        && connector3 != null
        && connector4 != null
      )
      {
        familyInstance = Doc.Create.NewCrossFitting(connector1, connector2, connector3, connector4);
      }
      else
      {
        var convAppObj = ConvertToNative(networkElement.elements) as ApplicationObject;
        foreach (var obj in convAppObj.Converted)
        {
          var nativeElement = obj as Element;
          appObj.Update(
            status: ApplicationObject.State.Created,
            createdId: nativeElement.UniqueId,
            convertedItem: nativeElement
          );
        }
      }

      if (familyInstance != null)
      {
        convertedElements.Add(networkElement.applicationId, familyInstance);
        familyInstance?.ChangeTypeId(familySymbol.Id);
        Doc.Delete(tempCurves.Select(c => c.Value.Id).ToList());

        appObj.Update(
          status: ApplicationObject.State.Created,
          createdId: familyInstance.UniqueId,
          convertedItem: familyInstance
        );
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
        var origin = PointToNative(link.origin);
        var firstConnector = GetConnectorByPoint(firstElement, origin);
        var secondConnector = GetConnectorByPoint(secondElement, origin);
        if (
          firstConnector != null
          && secondConnector != null
          && !firstConnector.IsConnected
          && !secondConnector.IsConnected
        )
        {
          firstConnector.ConnectTo(secondConnector);
        }
      }
    }

    return appObj;
  }

  public Network NetworkToSpeckle(Element mepElement, out List<string> notes)
  {
    Network speckleNetwork =
      new()
      {
        name = mepElement.Name,
        elements = new List<NetworkElement>(),
        links = new List<NetworkLink>()
      };

    GetNetworkElements(speckleNetwork, mepElement, out notes);

    return speckleNetwork;
  }

  /// <summary>
  /// Gets the connected element of a MEP element and adds the to a Base object
  /// </summary>
  /// <param name="base"></param>
  /// <param name="mepElement"></param>
  private void GetNetworkElements(Network @network, Element initialElement, out List<string> notes)
  {
    CachedContextObjects = ContextObjects;
    notes = new List<string>();
    var networkConnections = new List<ConnectionPair>();
    GetNetworkConnections(initialElement, ref networkConnections);
    var groups = networkConnections.GroupBy(n => n.Owner.UniqueId).ToList();

    foreach (var group in groups)
    {
      var element = Doc.GetElement(group.Key);

      if (ContextObjects.ContainsKey(element.UniqueId))
      {
        ContextObjects.Remove(element.UniqueId);
      }
      else
      {
        continue;
      }

      ApplicationObject reportObj = Report.ReportObjects.ContainsKey(element.UniqueId)
        ? Report.ReportObjects[element.UniqueId]
        : new ApplicationObject(element.UniqueId, element.GetType().ToString());

      Base obj = null;
      bool connectorBasedCreation = false;
      switch (element)
      {
        case DB.FamilyInstance fi:
          obj = FamilyInstanceToSpeckle(fi, out notes);
          // test if this family instance is a fitting
          var fittingCategories = new List<BuiltInCategory>
          {
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_CableTrayFitting,
            BuiltInCategory.OST_ConduitFitting
          };
          if (fittingCategories.Any(c => (int)c == fi.Category.Id.IntegerValue))
          {
            connectorBasedCreation = IsConnectorBasedCreation(fi);
            var partType = (PartType)
              fi.Symbol.Family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsInteger();
            if (obj != null)
            {
              obj["partType"] = partType.ToString();
            }
          }
          break;
        case DB.Plumbing.Pipe pipe:
          obj = PipeToSpeckle(pipe);
          break;
        case DB.Plumbing.FlexPipe flexpipe:
          obj = PipeToSpeckle(flexpipe);
          break;
        case DB.Mechanical.Duct duct:
          obj = DuctToSpeckle(duct, out notes);
          break;
        case DB.Mechanical.FlexDuct flexDuct:
          obj = DuctToSpeckle(flexDuct);
          break;
        case DB.Electrical.CableTray cableTray:
          obj = CableTrayToSpeckle(cableTray);
          break;
        case DB.Electrical.Conduit conduit:
          obj = ConduitToSpeckle(conduit);
          break;
        default:
          reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Conversion not supported");
          Report.Log(reportObj);
          continue;
      }

      if (obj == null)
      {
        reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
        Report.Log(reportObj);
        continue;
      }

      reportObj.Update(
        status: ApplicationObject.State.Created,
        logItem: $"Attached as connected element to {initialElement.UniqueId}"
      );
      @network.elements.Add(
        new RevitNetworkElement()
        {
          applicationId = element.UniqueId,
          name = element.Name,
          elements = obj,
          linkIndices = new List<int>(),
          isConnectorBased = connectorBasedCreation,
          isCurveBased = element is MEPCurve
        }
      );
      ConvertedObjects.Add(obj.applicationId);

      Report.Log(reportObj);
    }

    foreach (var group in groups)
    {
      var connections = group.ToList();
      var ownerIndex = @network.elements.FindIndex(e => e.applicationId.Equals(group.Key));
      var ownerElement = @network.elements[ownerIndex];
      foreach (var connection in connections)
      {
        var link = new RevitNetworkLink()
        {
          name = connection.Name,
          network = @network,
          elementIndices = new List<int>()
        };

        link.elementIndices.Add(ownerIndex);

        var connector = connection.Connector;

        link.domain = connector.Domain.ToString();
        link.shape = connector.Shape.ToString();
        link.systemName = connector.Owner.Category.Name;
        link.systemType = connector.MEPSystem != null ? Doc.GetElement(connector.MEPSystem.GetTypeId()).Name : "";

        var origin = connection.Connector.Origin;

        link.origin = PointToSpeckle(origin, initialElement.Document);
        link.fittingIndex = connector.Id;
        link.direction = VectorToSpeckle(connector.CoordinateSystem.BasisZ, initialElement.Document);
        link.isConnected = connection.IsConnected;
        link.needsPlaceholders = connection.ConnectedToCurve(out MEPCurve curve) && IsWithinContext(curve);
        link.diameter = connection.Diameter;
        link.height = connection.Height;
        link.width = connection.Width;

        // find index of the ref element
        var refConnector = connection.RefConnector;
        var refIndex = @network.elements.FindIndex(e => e.applicationId.Equals(refConnector?.Owner?.UniqueId));

        // add it in case it exists
        if (refIndex != -1)
        {
          link.elementIndices.Add(refIndex);
        }

        @network.links.Add(link);
        var linkIndex = @network.links.IndexOf(link);
        ownerElement.linkIndices.Add(linkIndex);
      }
    }

    if (@network.elements.Any())
    {
      notes.Add($"Converted and attached {@network.elements.Count} connected elements");
    }
  }

  private void GetNetworkConnections(Element element, ref List<ConnectionPair> networkConnections)
  {
    var connectionPairs = ConnectionPair.GetConnectionPairs(element);
    foreach (var connectionPair in connectionPairs)
    {
      if (!networkConnections.Contains(connectionPair))
      {
        networkConnections.Add(connectionPair);
        var refElement = connectionPair.RefConnector?.Owner;
        if (connectionPair.IsConnected && IsWithinContext(refElement))
        {
          GetNetworkConnections(refElement, ref networkConnections);
        }
      }
    }
  }

  // for fitting family instances, retrieves the type of fitting and determines if it is connector based
  private bool IsConnectorBasedCreation(DB.FamilyInstance familyInstance)
  {
    var connectors = GetConnectors(familyInstance).Cast<Connector>().ToArray();
    return connectors.All(
      c =>
        connectors.All(
          c1 =>
            (c1.Domain == Domain.DomainPiping && c1.PipeSystemType == c.PipeSystemType)
            || (c1.Domain == Domain.DomainHvac && c1.DuctSystemType == c.DuctSystemType)
            || (c1.Domain == Domain.DomainElectrical && c1.ElectricalSystemType == c.ElectricalSystemType)
            || (c1.Domain == Domain.DomainCableTrayConduit)
        )
    );
  }

  private static Dictionary<string, ApplicationObject> CachedContextObjects = null;

  private bool IsWithinContext(Element element)
  {
    return CachedContextObjects.ContainsKey(element?.UniqueId);
  }

  private void GetConnectionPairs(
    Element element,
    ref List<Tuple<Connector, Connector, Element>> connectionPairs,
    ref List<Element> elements
  )
  {
    var refss = ConnectionPair.GetConnectionPairs(element);

    foreach (var r in refss)
    {
      var isValid = r.IsValid();
      var isConnected = r.IsConnected;
    }
    var refs = GetRefConnectionPairs(element);
    var refConnectionPairs = GetRefConnectionPairs(element)
      .Where(e => e.Item2 == null || ContextObjects.ContainsKey(e.Item2.Owner.UniqueId))
      .ToList();
    elements.Add(element);
    foreach (var refConnectionPair in refs)
    {
      var connectedElement = refConnectionPair.Item2?.Owner;
      if (
        connectedElement != null
        && !elements.Any(e => e.UniqueId.Equals(connectedElement.UniqueId))
        && ContextObjects.ContainsKey(connectedElement.UniqueId)
      )
      {
        connectionPairs.Add(Tuple.Create(refConnectionPair.Item1, refConnectionPair.Item2, element));
        GetConnectionPairs(connectedElement, ref connectionPairs, ref elements);
      }
      else
      {
        connectionPairs.Add(Tuple.Create<Connector, Connector, Element>(refConnectionPair.Item1, null, element));
      }
    }
  }

  private static List<Tuple<Connector, Connector>> GetRefConnectionPairs(Element element)
  {
    var refConnectionPairs = new List<Tuple<Connector, Connector>>();
    var connectors = GetConnectors(element);
    var connectorsIterator = connectors.ForwardIterator();
    connectorsIterator.Reset();
    while (connectorsIterator.MoveNext())
    {
      var connector = connectorsIterator.Current as Connector;
      if (connector != null && connector.IsConnected)
      {
        var refs = connector.AllRefs;
        var refsIterator = refs.ForwardIterator();
        refsIterator.Reset();
        while (refsIterator.MoveNext())
        {
          var refConnector = refsIterator.Current as Connector;
          if (refConnector != null && !refConnector.Owner.Id.Equals(element.Id) && !(refConnector.Owner is MEPSystem))
          {
            refConnectionPairs.Add(Tuple.Create(connector, refConnector));
          }
        }
      }
      else
      {
        refConnectionPairs.Add(Tuple.Create<Connector, Connector>(connector, null));
      }
    }
    return refConnectionPairs;
  }

  private static ConnectorSet GetConnectors(Element e)
  {
    return e is MEPCurve curve
      ? curve.ConnectorManager.Connectors
      : (e as DB.FamilyInstance)?.MEPModel?.ConnectorManager?.Connectors ?? new ConnectorSet();
  }

  private Connector GetConnectorByPoint(Element element, XYZ point)
  {
    switch (element)
    {
      case MEPCurve o:
        return o.ConnectorManager.Connectors
          .Cast<Connector>()
          .FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point, 0.00001));
      case DB.FamilyInstance o:
        return o.MEPModel?.ConnectorManager.Connectors
          .Cast<Connector>()
          .FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point, 0.00001));
      default:
        return null;
    }
  }

  private MEPCurve CreateCurve(RevitNetworkLink link)
  {
    var direction = VectorToNative(link.direction);
    var start = PointToNative(link.origin);
    var end = start.Add(direction.Multiply(2));

    var sfi =
      link.elements.FirstOrDefault(e => e.elements is BuiltElements.Revit.FamilyInstance)?.elements
      as BuiltElements.Revit.FamilyInstance;
    Level level = ConvertLevelToRevit(sfi.level, out ApplicationObject.State state);

    Domain domain = Enum.TryParse(link.domain, out domain) ? domain : Domain.DomainUndefined;
    ConnectorProfileType profile = Enum.TryParse(link.shape, out profile) ? profile : ConnectorProfileType.Invalid;

    MEPCurveType curveType = null;
    MEPCurve curve = null;
    switch (domain)
    {
      case Domain.DomainHvac:
        curveType = GetDefaultMEPCurveType(Doc, typeof(DuctType), profile);
        if (curveType == null)
        {
          goto default;
        }

        var mechanicalSystemType = new FilteredElementCollector(Doc)
          .WhereElementIsElementType()
          .OfClass(typeof(MechanicalSystemType))
          .ToElements()
          .Cast<MechanicalSystemType>()
          .FirstOrDefault(x => x.Name == link.systemType);
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
        curveType = GetDefaultMEPCurveType(Doc, typeof(PipeType), profile);
        if (curveType == null)
        {
          goto default;
        }

        var pipingSystemType = new FilteredElementCollector(Doc)
          .WhereElementIsElementType()
          .OfClass(typeof(PipingSystemType))
          .ToElements()
          .Cast<PipingSystemType>()
          .FirstOrDefault(x => x.Name == link.systemType);
        curve = Pipe.Create(Doc, pipingSystemType.Id, curveType.Id, level.Id, start, end);
        curve.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(link.diameter);
        break;
      case Domain.DomainCableTrayConduit:
        if (profile == ConnectorProfileType.Rectangular)
        {
          curveType = GetDefaultMEPCurveType(Doc, typeof(CableTrayType), profile);
          if (curveType == null)
          {
            goto default;
          }

          curve = CableTray.Create(Doc, curveType.Id, start, end, level.Id);
          curve.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).Set(link.width);
          curve.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).Set(link.height);
        }
        else
        {
          curveType = GetDefaultMEPCurveType(Doc, typeof(ConduitType), profile);
          if (curveType == null)
          {
            goto default;
          }

          curve = Conduit.Create(Doc, curveType.Id, start, end, level.Id);
          curve.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).Set(link.diameter);
        }
        break;
      default:
        return curve;
    }
    return curve;
  }

  private static MEPCurveType GetDefaultMEPCurveType(Document doc, Type type, ConnectorProfileType shape)
  {
    return new FilteredElementCollector(doc)
        .WhereElementIsElementType()
        .OfClass(type)
        .FirstOrDefault(t => t is MEPCurveType type && type.Shape == shape) as MEPCurveType;
  }
}
