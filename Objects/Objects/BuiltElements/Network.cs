using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

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
    [Obsolete("The Network was previously used to assemble MEP systems, but now MEP systems are assembled by the RevitCommitObjectBuilder as MEP elements are converted.")]
    public Network() { }
    public string name { get; set; }

    /// <summary>
    /// The elements contained in the network
    /// </summary>
    public List<NetworkElement> elements { get; set; }

    /// <summary>
    /// The connections between <see cref="elements"/>
    /// </summary>
    public List<NetworkLink> links { get; set; }
  }

  public class NetworkElement : Base
  {
    [Obsolete("The NetworkElement class is obsolete because the Network class is now obsolete")]
    public NetworkElement() { }
    public string name { get; set; }

    [DetachProperty]
    /// <summary>
    /// The Base object representing the element in the network (eg Pipe, Duct, etc)
    /// </summary>
    /// <remarks>
    /// Currently named "elements" to assist with receiving in connector flatten method.
    /// </remarks>
    public Base elements { get; set; }

    /// <summary>
    /// The index of the links in <see cref="network"/> that are connected to this element
    /// </summary>
    public List<int> linkIndices { get; set; }

    [JsonIgnore]
    public Network network { get; set; }

    /// <summary>
    /// Retrieves the links for this element
    /// </summary>
    [JsonIgnore]
    public List<NetworkLink> links => linkIndices.Select(i => network?.links[i]).ToList();
  }

  public class NetworkLink : Base
  {
    [Obsolete("The NetworkLink class is obsolete because the Network class is now obsolete")]
    public NetworkLink() { }
    public string name { get; set; }

    /// <summary>
    /// The index of the elements in <see cref="network"/> that are connected by this link
    /// </summary>
    public List<int> elementIndices { get; set; }

    [JsonIgnore]
    public Network network { get; set; }

    /// <summary>
    /// Retrieves the elements for this link
    /// </summary>
    [JsonIgnore]
    public List<NetworkElement> elements => elementIndices.Select(i => network?.elements[i]).ToList();
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitNetworkElement : NetworkElement
  {
    [Obsolete("The RevitNetworkElement class is obsolete because the Network class is now obsolete")]
    public RevitNetworkElement() { }
    /// <summary>
    /// Indicates if this element was constructed from an MEP curve
    /// </summary>
    public bool isCurveBased { get; set; }

    /// <summary>
    /// Indicates if this element needs temporary placeholder objects to be created first when receiving
    /// </summary>
    /// <remarks>
    /// For example, some fittings cannot be created based on connectors, and so will be created similarly to mechanical equipment
    /// </remarks>
    public bool isConnectorBased { get; set; }
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
    /// The connector profile shape of the <see cref="NetworkLink"/>
    /// </summary>
    public string shape { get; set; }

    /// <summary>
    /// The link domain
    /// </summary>
    public string domain { get; set; }

    /// <summary>
    /// The index indicating the position of this link on the connected fitting element, if applicable
    /// </summary>
    /// <remarks>
    /// Revit fitting links are 1-indexed. For example, "T" fittings will have ordered links from index 1-3.
    /// </remarks>
    public int fittingIndex { get; set; }

    /// <summary>
    /// Indicates if this link needs temporary placeholder objects to be created first when receiving
    /// </summary>
    /// <remarks>
    /// Placeholder geometry are curves.
    /// For example, U-bend links need temporary pipes to be created first, if one or more linked pipes have not yet been created in the network.
    /// </remarks>
    public bool needsPlaceholders { get; set; }

    /// <summary>
    /// Indicates if this link has been connected to its elements
    /// </summary>
    public bool isConnected { get; set; }
  }
}
