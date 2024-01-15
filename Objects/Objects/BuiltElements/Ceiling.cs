using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Ceiling : Base, IDisplayValue<List<Mesh>>
{
  public Ceiling() { }

  [SchemaInfo("Ceiling", "Creates a Speckle ceiling", "BIM", "Architecture")]
  public Ceiling(
    [SchemaMainParam] ICurve outline,
    List<ICurve>? voids = null,
    [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base>? elements = null
  )
  {
    this.outline = outline;
    this.voids = voids ?? new();
    this.elements = elements;
  }

  public ICurve outline { get; set; }
  public List<ICurve> voids { get; set; } = new();

  [DetachProperty]
  public List<Base>? elements { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
