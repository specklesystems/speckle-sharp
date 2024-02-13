using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Wire : Base
{
  public Wire() { }

  [SchemaInfo("Wire", "Creates a Speckle wire from curve segments and points", "BIM", "MEP")]
  public Wire(List<ICurve> segments)
  {
    this.segments = segments;
  }

  public List<ICurve> segments { get; set; }

  public string units { get; set; }
}
