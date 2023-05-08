#nullable enable
using System;
using System.Collections.Generic;

namespace Speckle.Core.Kits;

/// <summary>
/// Defines the basic interface for creating a "Speckle Kit"
/// </summary>
public interface ISpeckleKit
{
  /// <summary>
  /// Gets all the object types (the object model) provided by this kit.
  /// </summary>
  IEnumerable<Type> Types { get; }

  /// <summary>
  /// Gets all available converters for this Kit.
  /// </summary>
  IEnumerable<string> Converters { get; }

  /// <summary>
  /// Gets this Kit's description.
  /// </summary>
  string Description { get; }

  /// <summary>
  /// Gets this Kit's name.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Gets this Kit's author.
  /// </summary>
  string Author { get; }

  /// <summary>
  /// Gets the website (or email) to contact the Kit's author.
  /// </summary>
  string WebsiteOrEmail { get; }

  /// <summary>
  /// Tries to load a converter for a specific <paramref name="app"/>.
  /// </summary>
  /// <param name="app">The host app string for which a <see cref="ISpeckleConverter"/> is desired. see <see cref="ISpeckleConverter.GetServicedApplications"/></param>
  /// <returns>The converter for the specific <paramref name="app"/></returns>
  /// <exception cref="KitException">Thrown if the requested converter failed to load</exception>
  public ISpeckleConverter LoadConverter(string app);
}
