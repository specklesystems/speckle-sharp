using System.Collections.Generic;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitBeam : Beam
{
  public RevitBeam() { }

  [SchemaInfo("RevitBeam", "Creates a Revit beam by curve and base level.", "Revit", "Structure")]
  public RevitBeam(
    string family,
    string type,
    [SchemaMainParam] ICurve baseLine,
    Level level,
    List<Parameter>? parameters = null
  )
  {
    this.family = family;
    this.type = type;
    this.baseLine = baseLine;
    this.parameters = parameters?.ToBase();
    this.level = level;
  }

  public string family { get; set; }
  public string type { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }

  public new Level? level
  {
    get => base.level;
    set => base.level = value;
  }
}
