using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AdvanceSteelSpecialPart : AdvanceSteelObject
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    //[SchemaInfo("AdvanceSteelSpecialPart", "Creates a Advance Steel special part.", "Advance Steel", "Structure")]
    public AdvanceSteelSpecialPart()
    {

    }
  }
}
