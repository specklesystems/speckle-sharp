using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Civil;

public class CivilAppliedSubassembly : Base
{
  public CivilAppliedSubassembly() { }

  public CivilAppliedSubassembly(List<CivilCalculatedShape> shapes, List<Base> parameters)
  {
    this.shapes = shapes;
    this.parameters = parameters;
  }

  [DetachProperty]
  public List<Base> parameters { get; set; }

  public List<CivilCalculatedShape> shapes { get; set; }
}
