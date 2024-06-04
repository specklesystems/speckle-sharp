using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Civil;

public class CivilAppliedSubassembly : Base
{
  public CivilAppliedSubassembly() { }

  public CivilAppliedSubassembly(
    string subassemblyId,
    string subassemblyName,
    List<CivilCalculatedShape> shapes,
    List<Base> parameters
  )
  {
    this.subassemblyId = subassemblyId;
    this.subassemblyName = subassemblyName;
    this.shapes = shapes;
    this.parameters = parameters;
  }

  public string subassemblyId { get; set; }

  public string subassemblyName { get; set; }

  [DetachProperty]
  public List<Base> parameters { get; set; }

  public List<CivilCalculatedShape> shapes { get; set; }
}
