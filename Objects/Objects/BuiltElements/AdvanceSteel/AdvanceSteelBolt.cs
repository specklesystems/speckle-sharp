using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

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
    [SchemaInfo("AdvanceSteelCircularBolt", "Creates a Advance Steel circular bolt.", "Advance Steel", "Structure")]
    public AdvanceSteelCircularBolt()
    {

    }
  }

  public class AdvanceSteelRectangularBolt : AdvanceSteelBolt
  {
    [SchemaInfo("AdvanceSteelRectangularBolt", "Creates a Advance Steel rectangular bolt.", "Advance Steel", "Structure")]
    public AdvanceSteelRectangularBolt()
    {

    }
  }
}
