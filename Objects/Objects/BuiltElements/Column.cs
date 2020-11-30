using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Column : Base
  {
    public ICurve baseLine { get; set; }

    public Column() { }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitColumn : Column
  {
    [SchemaOptional]
    public Level topLevel { get; set; }

    [SchemaOptional]
    public double baseOffset { get; set; }

    [SchemaOptional]
    public double topOffset { get; set; }

    [SchemaOptional]
    public bool facingFlipped { get; set; }

    [SchemaOptional]
    public bool handFlipped { get; set; }

    [SchemaOptional]
    public bool structural { get; set; }

    [SchemaOptional]
    public double rotation { get; set; }

    [SchemaIgnore]
    public bool isSlanted { get; set; }

    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public Level level { get; set; }

  }
}
