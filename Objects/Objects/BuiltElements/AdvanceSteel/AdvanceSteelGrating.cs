using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AdvanceSteelGrating : AdvanceSteelObject
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    //[SchemaInfo("AdvanceSteelGrating", "Creates a Advance Steel grating.", "Advance Steel", "Structure")]
    public AdvanceSteelGrating()
    {

    }
  }
}
