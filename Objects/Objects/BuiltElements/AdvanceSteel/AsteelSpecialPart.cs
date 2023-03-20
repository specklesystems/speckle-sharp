using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.AdvanceSteel
{
  public class AsteelSpecialPart : AsteelObject
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    //[SchemaInfo("AsteelSpecialPart", "Creates a Advance Steel special part.", "Advance Steel", "Structure")]
    public AsteelSpecialPart()
    {

    }
  }
}
