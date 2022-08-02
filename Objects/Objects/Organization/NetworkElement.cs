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
  public class NetworkElement : Base
  {
    public string name { get; set; }

    public Base element {get; set;}

    /// <summary>
    /// The index of the links in <see cref="network"/> that belong to this element
    /// </summary>
    public List<int> linkIndices { get; set; }

    [JsonIgnore] public Network network { get; set; }

    public NetworkElement() { }

    /// <summary>
    /// Retrieves the links for this element
    /// </summary>
    [JsonIgnore] public List<NetworkLink> links => linkIndices.Select(i => network.links[i]).ToList();
  }
}
namespace Objects.Organization.Revit
{
  public class RevitNetworkElement : NetworkElement
  {
    public RevitNetworkElement() { }

  }
}
  

  

