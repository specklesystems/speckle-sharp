using Speckle.Core.Kits;
using Objects.BuiltElements.TeklaStructures;
using Objects.Geometry;
using Objects.Structural;

namespace Objects.Properties
{
  /// <summary>
  /// Container for holding Revit-specific properties and settable Built-in Parameters (on the <see cref="ApplicationProperties.props"/> field).
  /// A <see cref="RevitProperties"/> object gets automatically created and attached to the <see cref="PhysicalElement.sourceApp"/> field on
  /// any <see cref="PhysicalElement"/> sent from Revit. It can also be attached to custom objects to create near Revit-native
  /// objects programatically.
  /// </summary>
  public class TeklaStructuresProperties : ApplicationProperties
  {
    public new string name => HostApplications.TeklaStructures.Name;
    public TeklaPosition teklaPosition { get; set; }
    public Vector alignmnetVector { get; set; }
    public Structural.Properties.Profiles.SectionProfile profile { get; set; }
    public Structural.Materials.Material material { get; set; }
    public string finish { get; set; }
    public string classNumber { get; set; }
  }
}