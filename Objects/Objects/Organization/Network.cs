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
  /// <summary>
  /// Represents graph connections between objects
  /// </summary>
  /// <remarks>
  /// Network <see cref="elements"/> may need to be created first in native applications before they are linked.
  /// </remarks>
  public class Network : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The elements contained
    /// </summary>
    public List<NetworkElement> elements { get; set; }

    /// <summary>
    /// The connections between <see cref="elements"/>
    /// </summary>
    public List<NetworkLink> links { get; set; }

    public Network() { }

  }
}
