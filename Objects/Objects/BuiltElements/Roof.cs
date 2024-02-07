using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Roof : Base, IDisplayValue<List<Mesh>>
{
  public Roof() { }

  [SchemaDeprecated, SchemaInfo("Roof", "Creates a Speckle roof", "BIM", "Architecture")]
  public Roof([SchemaMainParam] ICurve outline, List<ICurve>? voids = null, List<Base>? elements = null)
  {
    this.outline = outline;
    this.voids = voids ?? new();
    this.elements = elements;
  }

  public ICurve outline { get; set; }
  public virtual Level? level { get; internal set; }
  public List<ICurve> voids { get; set; } = new();

  [DetachProperty]
  public List<Base>? elements { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
