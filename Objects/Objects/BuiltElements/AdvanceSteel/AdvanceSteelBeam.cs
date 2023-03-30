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
  public class AdvanceSteelBeam : AdvanceSteelObject, IDisplayValue<List<Mesh>>, IHasVolume, IHasArea
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }
    public string name { get; set; }
    [DetachProperty]
    public SectionProfile profile { get; set; }
    [DetachProperty]
    public StructuralMaterial material { get; set; }
    [DetachProperty]
    public string finish { get; set; }
    [DetachProperty]
    public string classNumber { get; set; }

    public Base userProperties { get; set; }
    public AdvanceSteelBeamType AdvanceSteelBeamType { get; set; }
    public double volume { get; set; }
    public double area { get; set; }

    public AdvanceSteelBeam() { }

    [SchemaInfo("AdvanceSteelBeam", "Creates a Advance Steel beam by curve.", "Advance Steel", "Structure")]
    public AdvanceSteelBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, StructuralMaterial material)
    {
      this.baseLine = baseLine;
      this.profile = profile;
      this.material = material;
    }
  }
}
