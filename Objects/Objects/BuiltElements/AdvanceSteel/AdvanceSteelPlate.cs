using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AdvanceSteelPlate : Area
  {
    [DetachProperty]
    public StructuralMaterial material { get; set; }

    [SchemaInfo("AdvanceSteelPlate", "Creates a Advance Steel plate.", "Advance Steel", "Structure")]
    public AdvanceSteelPlate(SectionProfile profile, Polyline outline, string units, StructuralMaterial material = null)
    {
      this.outline = outline;
      this.material = material;
      this.units = units;
    }


    public AdvanceSteelPlate() { }
  }
}
