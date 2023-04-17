using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

public class AsteelSlab : Area, IDisplayValue<List<Mesh>>, IHasArea, IHasVolume, IAsteelObject
{
  [SchemaInfo("AsteelSlab", "Creates a Advance Steel slab.", "Advance Steel", "Structure")]
  public AsteelSlab(Polyline outline, string units, StructuralMaterial material = null)
  {
    this.outline = outline;
    this.material = material;
    this.units = units;
  }

  public AsteelSlab() { }

  [DetachProperty]
  public StructuralMaterial material { get; set; }

  public Base userAttributes { get; set; }
}
