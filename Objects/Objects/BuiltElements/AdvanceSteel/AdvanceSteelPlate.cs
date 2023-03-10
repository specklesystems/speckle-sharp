using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using System;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

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
