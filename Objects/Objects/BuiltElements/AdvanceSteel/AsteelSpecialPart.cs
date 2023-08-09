using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

public class AsteelSpecialPart : Base, IAsteelObject
{
  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public Base userAttributes { get; set; }

  public Base asteelProperties { get; set; }

  //[SchemaInfo("AsteelSpecialPart", "Creates a Advance Steel special part.", "Advance Steel", "Structure")]
  public AsteelSpecialPart()
  {

  }
}
