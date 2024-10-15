using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

// TODO: This class really shouldn't inherit from Area, but we need to fix the inheritance chain in the future.
public class AsteelSlab : Area, IDisplayValue<List<Base>>, IHasArea, IHasVolume, IAsteelObject
{
  [DetachProperty]
  public StructuralMaterial? material { get; set; }

  public Base userAttributes { get; set; }

  public Base asteelProperties { get; set; }

  [SchemaInfo("AsteelSlab", "Creates a Advance Steel slab.", "Advance Steel", "Structure")]
  public AsteelSlab(Polyline outline, string units, StructuralMaterial? material = null)
  {
    this.outline = outline;
    this.material = material;
    this.units = units;
  }

  public AsteelSlab() { }
}
