using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Brace : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Brace() { }

    [SchemaInfo("Brace", "Creates a Speckle brace", "BIM", "Structure")]
    public Brace([SchemaMainParam] ICurve baseLine)
    {
      this.baseLine = baseLine;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitBrace : Brace
  {
    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBrace() { }

    [SchemaInfo("RevitBrace", "Creates a Revit brace by curve and base level.", "Revit", "Structure")]
    public RevitBrace(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.parameters = parameters.ToBase();
      this.level = level;
    }
  }
}
