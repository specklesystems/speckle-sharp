using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements;

namespace Objects.BuiltElements
{
  /// <summary>
  /// Represents graph connections between built elements objects
  /// </summary>
  /// <remarks>
  /// Network <see cref="elements"/> may need to be created first in native applications before they are linked.
  /// </remarks>
  public class Network : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The elements contained in the network
    /// </summary>
    public List<NetworkElement> elements { get; set; }

    /// <summary>
    /// The connections between <see cref="elements"/>
    /// </summary>
    public List<NetworkLink> links { get; set; }

    public Network() { }
  }

  public class NetworkElement : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The Base object representing the element in the network (eg Pipe, Duct, etc)
    /// </summary>
    public Base element { get; set; }

    /// <summary>
    /// The index of the links in <see cref="network"/> that are connected to this element
    /// </summary>
    public List<int> linkIndices { get; set; }

    [JsonIgnore] public Network network { get; set; }

    public NetworkElement() { }

    /// <summary>
    /// Retrieves the links for this element
    /// </summary>
    [JsonIgnore] public List<NetworkLink> links => linkIndices.Select(i => network.links[i]).ToList();
  }

  public class NetworkLink : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The index of the elements in <see cref="network"/> that are connected by this link
    /// </summary>
    public List<int> elementIndices { get; set; }

    [JsonIgnore] public Network network { get; set; }

    public NetworkLink() { }

    /// <summary>
    /// Retrieves the elements for this link
    /// </summary>
    [JsonIgnore] public List<NetworkElement> elements => elementIndices.Select(i => network.elements[i]).ToList();
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitNetworkElement : NetworkElement
  {
    public FittingType fittingType { get; set; }
    public bool connectorBasedCreation { get; set; }
    public bool isCurve { get; set; }

    public RevitNetworkElement() { }
  }

  public class RevitNetworkLink : NetworkLink
  {
    public double height { get; set; }
    public double width { get; set; }
    public double diameter { get; set; }
    public Point origin { get; set; }
    public Vector direction { get; set; }
    /// <summary>
    /// The system category
    /// </summary>
    public string systemName { get; set; }
    public string systemType { get; set; }

    /// <summary>
    /// The shape of the <see cref="NetworkLink"/>
    /// </summary>
    public RevitNetworkLinkShape shape { get; set; }

    /// <summary>
    /// The connector domain. Could be duct, piping, conduit, cabletray, or unknown 
    /// </summary>
    public string domain { get; set; }
    public int connectionIndex { get; set; }
    public bool connectedToCurve { get; set; }
    public bool connected { get; set; }

    public RevitNetworkLink() { }
  }

  /// <summary>
  /// Represents the shape of a <see cref="NetworkLink"/>.
  /// </summary>
  public enum RevitNetworkLinkShape
  {
    Unknown,
    Round,
    Rectangular,
    Oval
  }
}
