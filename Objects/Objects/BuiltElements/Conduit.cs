using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Conduit : Base, IDisplayValue<List<Mesh>>
{
  public ICurve baseCurve { get; set; }
  public double diameter { get; set; }
  public double length { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
