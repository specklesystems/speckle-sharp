using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaDescription("A Revit brace by line")]
  public class RevitBrace : Base, IRevitHasFamilyAndType, IBrace
  {
    public ICurve baseLine { get; set; }

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
