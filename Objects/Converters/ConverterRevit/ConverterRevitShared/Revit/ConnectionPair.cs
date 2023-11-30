using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace ConverterRevitShared.Revit;

public class ConnectionPair : IComparable<ConnectionPair>, IEquatable<ConnectionPair>
{
  public Element Owner { get; private set; }

  public Connector Connector { get; private set; }

  public Connector RefConnector { get; private set; }

  public double Diameter { get; private set; }

  public double Height { get; private set; }

  public double Width { get; private set; }

  public bool IsConnected { get; private set; }

  public string Name =>
    RefConnector != null
      ? $"{Owner.Category.Name}: {Connector.Owner.Name} --> {RefConnector.Owner.Category.Name}: {RefConnector.Owner.Name}"
      : $"{Owner.Category.Name}: {Connector.Owner.Name} --> NULL";

  private ConnectionPair(Element element, Connector connector, Connector refConnector = null)
  {
    Owner = element;
    Connector = connector;
    RefConnector = refConnector;
    IsConnected = RefConnector != null;
    var curve = element as MEPCurve;
    switch (Connector.Shape)
    {
      case ConnectorProfileType.Invalid:
        break;
      case ConnectorProfileType.Round:
        Diameter = curve != null ? curve.Diameter : Connector.Radius * 2;
        break;
      default:
        Height = curve != null ? curve.Height : Connector.Height;
        Width = curve != null ? curve.Width : Connector.Width;
        break;
    }
  }

  public static ICollection<ConnectionPair> GetConnectionPairs(Element element)
  {
    var connectionPairs = new List<ConnectionPair>();
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
            connectionPairs.Add(new ConnectionPair(element, connector, refConnector));
          }
        }
      }
      else
      {
        connectionPairs.Add(new ConnectionPair(element, connector, null));
      }
    }
    return connectionPairs;
  }

  public bool IsValid()
  {
    return Connector.Owner.Id.Equals(Owner.Id) && RefConnector != null ? Connector.IsConnectedTo(RefConnector) : true;
  }

  public int CompareTo(ConnectionPair other)
  {
    return Convert.ToInt32(Equals(other));
  }

  public bool Equals(ConnectionPair other)
  {
    return Owner.Id.Equals(other.Owner.Id) && RefConnector?.Owner?.Id == other.RefConnector?.Owner?.Id;
  }

  public bool ConnectedToCurve(out MEPCurve curve)
  {
    curve = RefConnector?.Owner as MEPCurve;
    return curve != null;
  }

  private static ConnectorSet GetConnectors(Element e)
  {
    return e is MEPCurve cure
      ? cure.ConnectorManager.Connectors
      : (e as FamilyInstance)?.MEPModel?.ConnectorManager?.Connectors ?? new ConnectorSet();
  }
}
