using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

public class AsteelGrating : Base, IAsteelObject
{
  //[SchemaInfo("AsteelGrating", "Creates a Advance Steel grating.", "Advance Steel", "Structure")]

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public Base userAttributes { get; set; }
}
