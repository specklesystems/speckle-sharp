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
    /// The index of the network elements that this link connects
    /// </summary>
    public List<int> connections { get; set; }

    public NetworkLink() { }
  }
}
namespace Objects.Organization.Revit
{
  public class RevitNetworkLink : Base
  {
    /// <summary>
    /// The shape of the <see cref="NetworkLink"/> at each <see cref="NetworkLink.connections"/>
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
  

  

