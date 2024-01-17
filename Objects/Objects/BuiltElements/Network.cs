using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements;

/// <summary>
/// Represents graph connections between built elements objects
/// </summary>
/// <remarks>
/// Network <see cref="elements"/> may need to be created first in native applications before they are linked.
/// </remarks>
[Obsolete("Networks are no longer used in any connector to assemble MEP systems.")]
public class Network : Base
{
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

[Obsolete("Networks are no longer used in any connector to assemble MEP systems.")]
public class NetworkElement : Base
{
  public NetworkElement() { }

  public string name { get; set; }

  /// <summary>
  /// The Base object representing the element in the network (eg Pipe, Duct, etc)
  /// </summary>
  /// <remarks>
  /// Currently named "elements" to assist with receiving in connector flatten method.
  /// </remarks>
  [DetachProperty]
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
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type. Reason: obsolete.
  public List<NetworkLink> links => linkIndices.Select(i => network?.links[i]).ToList();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type. Reason: obsolete.
}

[Obsolete("Networks are no longer used in any connector to assemble MEP systems.")]
public class NetworkLink : Base
{
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
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type. Reason: obsolete.
  public List<NetworkElement> elements => elementIndices.Select(i => network?.elements[i]).ToList();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type. Reason: obsolete.
}
