using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

public class AsteelSpecialPart : Base, IAsteelObject
{
  //[SchemaInfo("AsteelSpecialPart", "Creates a Advance Steel special part.", "Advance Steel", "Structure")]

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public Base userAttributes { get; set; }
}
