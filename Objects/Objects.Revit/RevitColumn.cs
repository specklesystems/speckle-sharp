using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaDescription("A Revit column by line")]
  public class RevitColumn : RevitElement, IColumn
  {
    public double height { get; set; }
    public ICurve baseLine { get; set; }
    public RevitLevel topLevel { get; set; }

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

  }
}
