using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitBrace : Brace
{
  public RevitBrace() { }

  public RevitBrace(
    string family,
    string type,
    ICurve baseLine,
    Level? level,
    string? units,
    string? elementId,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Parameter>? parameters = null
  )
    : base(baseLine, units, displayValue)
  {
    this.family = family;
    this.type = type;
    this.level = level;
    this.elementId = elementId;
    this.parameters = parameters?.ToBase();
  }

  public string family { get; set; }
  public string type { get; set; }
  public Base? parameters { get; set; }
  public string? elementId { get; set; }
  public Level? level { get; set; }

  #region Schema Info Constructor

  [SchemaInfo("RevitBrace", "Creates a Revit brace by curve and base level.", "Revit", "Structure")]
  public RevitBrace(
    string family,
    string type,
    [SchemaMainParam] ICurve baseLine,
    Level level,
    List<Parameter>? parameters = null
  )
    : this(family, type, baseLine, level, null, null, parameters: parameters) { }

  #endregion
}
