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

namespace Objects.Organization
{
  public class NetworkElement : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The Base object representing the element in the network (eg Pipe, Duct, etc)
    /// </summary>
    public Base element {get; set;}

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
}
namespace Objects.Organization.Revit
{
  public class RevitNetworkElement : NetworkElement
  {
    public FittingType FittingType { get; set; }
    public RevitNetworkElement() { }

    public bool ConnectorBasedCreation()
    {
      return FittingType == FittingType.Elbow
        || FittingType == FittingType.Tee
        || FittingType == FittingType.Union
        || FittingType == FittingType.Transition
        || FittingType == FittingType.Cross
        || FittingType == FittingType.Tap;
    }
  }

  public enum FittingType
  {
    Elbow = 0,
    Tee = 1,
    Union = 2,
    Transition = 3,
    Cross = 4,
    Tap = 5,
    Other = 6,
    Invalid = -1
  }
}
  

  

