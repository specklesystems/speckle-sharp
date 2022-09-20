using Speckle.Newtonsoft.Json;
using System;

namespace DesktopUI2.Models.Settings
{
  public interface ISetting
  {
    /// <summary>
    /// User friendly name displayed in the UI
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// MaterialDesignIcon use the demo app from the MaterialDesignInXamlToolkit to get the correct name
    /// </summary>
    string Icon { get; set; }

    /// <summary>
    /// Internal setting name 
    /// </summary>
    string Slug { get; set; }

    /// <summary>
    /// Used as the discriminator for deserialisation.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Shoud return a succint summary of the setting: what does it contain inside?
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Should contain a generic description of the setting and what it changes.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Holds the value that the user selected from the setting.
    /// </summary>    
    string Selection { get; set; }

    /// <summary>
    /// View associated to this filter type
    /// </summary>    
    [JsonIgnore]
    Type ViewType { get; }


  }
}
