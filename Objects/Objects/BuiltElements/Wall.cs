using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Wall : Base, IDisplayValue<IReadOnlyList<Base>>
{
  public Wall() { }

  public Wall(
    double height,
    string? units,
    ICurve baseLine,
    Level? level = null,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Base>? elements = null
  )
  {
    this.height = height;
    this.units = units;
    this.baseLine = baseLine;
    this.level = level;
    this.displayValue = ((IReadOnlyList<Base>?)displayValue) ?? new[] { (Base)baseLine };
    this.elements = elements;
  }

  public double height { get; set; }

  public string? units { get; set; }
  public ICurve baseLine { get; set; }
  public virtual Level? level { get; internal set; }

  [DetachProperty]
  public List<Base>? elements { get; set; }

  [DetachProperty]
  public IReadOnlyList<Base> displayValue { get; set; }

  #region SchemaInfo Ctors

  [SchemaInfo("Wall", "Creates a Speckle wall", "BIM", "Architecture")]
  public Wall(
    double height,
    [SchemaMainParam] ICurve baseLine,
    [SchemaParamInfo("Any nested elements that this wall might have")] List<Base>? elements = null
  )
    : this(height, null, baseLine, null, null, elements) { }

  #endregion
}
