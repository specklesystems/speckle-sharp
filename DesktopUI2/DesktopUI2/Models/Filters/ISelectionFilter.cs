using System;
using System.Collections.Generic;
using Speckle.Newtonsoft.Json;

namespace DesktopUI2.Models.Filters;

public interface ISelectionFilter
{
  /// <summary>
  /// User friendly name displayed in the UI
  /// </summary>
  string Name { get; set; }

  /// <summary>
  /// Used as the discriminator for deserialization.
  /// </summary>
  string Type { get; }

  /// <summary>
  /// MaterialDesignIcon use the demo app from the MaterialDesignInXamlToolkit to get the correct name
  /// </summary>
  string Icon { get; set; }

  /// <summary>
  /// Internal filter name
  /// </summary>
  string Slug { get; set; }

  /// <summary>
  /// Should return a succinct summary of the filter: what does it contain inside?
  /// </summary>
  string Summary { get; }

  /// <summary>
  /// Should contain a generic description of the filter and how it works.
  /// </summary>
  string Description { get; set; }

  /// <summary>
  /// Holds the values that the user selected from the filter. Not the actual objects.
  /// </summary>
  List<string> Selection { get; set; }

  /// <summary>
  /// View associated to this filter type
  /// </summary>
  [JsonIgnore]
  Type ViewType { get; }
}
