using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel
{
  public abstract class AdvanceSteelBolt : AdvanceSteelObject
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public AdvanceSteelBolt()
    {

    }
  }

  public class AdvanceSteelCircularBolt : AdvanceSteelBolt
  {
    //[SchemaInfo("AdvanceSteelCircularBolt", "Creates a Advance Steel circular bolt.", "Advance Steel", "Structure")]
    public AdvanceSteelCircularBolt()
    {

    }
  }

  public class AdvanceSteelRectangularBolt : AdvanceSteelBolt
  {
    //[SchemaInfo("AdvanceSteelRectangularBolt", "Creates a Advance Steel rectangular bolt.", "Advance Steel", "Structure")]
    public AdvanceSteelRectangularBolt()
    {

    }
  }
}
