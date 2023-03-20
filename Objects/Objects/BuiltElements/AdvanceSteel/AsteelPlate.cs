using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AsteelPlate : AsteelObject, IHasArea, IHasVolume, IDisplayValue<List<Mesh>>
  {
    public double area { get; set; }
    public double volume { get; set; }
    public Point center { get; set; }
    public ICurve outline { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public StructuralMaterial material { get; set; }

    [SchemaInfo("AsteelPlate", "Creates a Advance Steel plate.", "Advance Steel", "Structure")]
    public AsteelPlate(SectionProfile profile, Polyline outline, string units, StructuralMaterial material = null)
    {
      this.outline = outline;
      this.material = material;
      this.units = units;
    }


    public AsteelPlate()
    {
    }
  }
}
