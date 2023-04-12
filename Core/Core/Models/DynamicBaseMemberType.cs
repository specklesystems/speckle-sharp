using System;

namespace Speckle.Core.Models;

/// <summary>
/// Represents all different types of members that can be returned by <see cref="DynamicBase.GetMembers"/>
/// </summary>
[Flags]
public enum DynamicBaseMemberType
{
  /// <summary>
  /// The typed members of the DynamicBase object
  /// </summary>
  Instance = 1,

  /// <summary>
  /// The dynamically added members of the DynamicBase object
  /// </summary>
  Dynamic = 2,

  /// <summary>
  /// The typed members flagged with <see cref="ObsoleteAttribute"/> attribute.
  /// </summary>
  Obsolete = 4,

  /// <summary>
  /// The typed members flagged with <see cref="Speckle.Core.Kits.SchemaIgnore"/> attribute.
  /// </summary>
  SchemaIgnored = 8,

  /// <summary>
  /// The typed methods flagged with TODO:
  /// </summary>
  SchemaComputed = 16,

  /// <summary>
  /// All the typed members, including ones with <see cref="ObsoleteAttribute"/> or <see cref="Speckle.Core.Kits.SchemaIgnore"/> attributes.
  /// </summary>
  InstanceAll = Instance + Obsolete + SchemaIgnored,

  /// <summary>
  /// All the members, including dynamic and instance members flagged with <see cref="ObsoleteAttribute"/> or <see cref="Speckle.Core.Kits.SchemaIgnore"/> attributes
  /// </summary>
  All = InstanceAll + Dynamic
}
