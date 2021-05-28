using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Brace : Base, IDisplayMesh
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public Brace() { }

    [SchemaInfo("Brace", "Creates a Speckle brace")]
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
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBrace() { }

    [SchemaInfo("RevitBrace", "Creates a Revit brace by curve and base level.")]
    public RevitBrace(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.parameters = parameters;
      this.level = level;
    }
  }
}
