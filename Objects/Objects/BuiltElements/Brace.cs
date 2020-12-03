using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Brace : Base
  {
    public ICurve baseLine { get; set; }

    public Brace() { }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitBrace : Brace
  {
    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public Level level { get; set; }
  }
}
