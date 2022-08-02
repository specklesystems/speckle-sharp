using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.Organization
{
  public class NetworkLink : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The index of the elements in <see cref="network"/> that belong to this link
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
namespace Objects.Organization.Revit
{
  public class RevitNetworkLink : NetworkLink
  {
    /// <summary>
    /// The shape of the <see cref="NetworkLink"/> at each <see cref="NetworkLink.elementIndices"/>
    /// </summary>
    public List<NetworkLinkShape> shapes { get; set; } // should this just be one shape or can a link have 2?

    public Vector direction { get; set; }

    public bool connected { get; set; }

    public double height { get; set; }

    public double width { get; set; }

    public double radius { get; set; }

    /// <summary>
    /// The system type
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// The system category
    /// </summary>
    public string category { get; set; }
  }

  /// <summary>
  /// Represents the shape of a <see cref="NetworkLink"/>.
  /// </summary>
  public enum NetworkLinkShape
  {
    Unknown,
    Rectangular,
    Round,
    Square
  }
}
  

  

