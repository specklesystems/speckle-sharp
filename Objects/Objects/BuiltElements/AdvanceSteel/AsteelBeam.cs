using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AsteelBeam : AsteelObject, IDisplayValue<List<Mesh>>, IHasVolume, IHasArea
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }
    [DetachProperty]
    public SectionProfile profile { get; set; }
    [DetachProperty]
    public StructuralMaterial material { get; set; }

    [DetachProperty]
    public AsteelSectionProfile asteelProfile { get; set; }

    public double volume { get; set; }
    public double area { get; set; }

    public AsteelBeam() { }

    [SchemaInfo("AsteelBeam", "Creates a Advance Steel beam by curve.", "Advance Steel", "Structure")]
    public AsteelBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, StructuralMaterial material)
    {
      this.baseLine = baseLine;
      this.profile = profile;
      this.material = material;
    }
  }
}
