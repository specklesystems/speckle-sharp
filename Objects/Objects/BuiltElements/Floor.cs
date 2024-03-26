using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Floor : Base, IDisplayValue<List<Mesh>>
{
  public Floor() { }

  [SchemaInfo("Floor", "Creates a Speckle floor", "BIM", "Architecture")]
  public Floor(
    [SchemaMainParam] ICurve outline,
    List<ICurve>? voids = null,
    [SchemaParamInfo("Any nested elements that this floor might have")] List<Base>? elements = null
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
  public virtual Level? level { get; internal set; }
  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
