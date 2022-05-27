using Speckle.Core.Kits;

namespace Objects.Properties
{
  /// <summary>
  /// Container for holding Revit-specific properties and settable Built-in Parameters (on the <see cref="ApplicationProperties.props"/> field).
  /// A <see cref="RevitProperties"/> object gets automatically created and attached to the <see cref="PhysicalElement.sourceApp"/> field on
  /// any <see cref="PhysicalElement"/> sent from Revit. It can also be attached to custom objects to create near Revit-native
  /// objects programatically.
  /// </summary>
  public class RevitProperties : ApplicationProperties
  {
    public new string name => HostApplications.Revit.Name;
    public string family { get; set; }
    public string type { get; set; }
    public string elementId { get; set; }
  }
}