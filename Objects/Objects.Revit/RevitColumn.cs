using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaDescription("A Revit column by line")]
  public class RevitColumn : Base, IRevitHasFamilyAndType, IColumn
  {
    public double height { get; set; }

    public ICurve baseLine { get; set; }

    [SchemaOptional]
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
    public ILevel level { get; set; }

  }
}
