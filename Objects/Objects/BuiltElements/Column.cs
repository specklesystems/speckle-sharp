using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Column : Base, IDisplayValue<IReadOnlyList<Base>>
{
  public Column() { }

  public Column(ICurve baseLine, string? units, Level? level = null, IReadOnlyList<Mesh>? displayValue = null)
  {
    this.baseLine = baseLine;
    this.units = units;
    this.level = level;
    this.displayValue = ((IReadOnlyList<Base>?)displayValue) ?? new[] { (Base)baseLine };
  }

  public ICurve baseLine { get; set; }

  public virtual Level? level { get; internal set; }

  public string? units { get; set; }

  [DetachProperty]
  public IReadOnlyList<Base> displayValue { get; set; }

  #region Schema Info Constructors

  [SchemaInfo("Column", "Creates a Speckle column", "BIM", "Structure")]
  [SchemaDeprecated, Obsolete("Use other constructor")]
  public Column([SchemaMainParam] ICurve baseLine)
    : this(baseLine, null) { }
  #endregion
}
