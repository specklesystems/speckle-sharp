using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaDescription("A Revit beam by line")]
  public class RevitBeam : Base, IRevitHasFamilyAndType, IRevitHasParameters, IRevitHasTypeParameters, IBeam
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
