using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitFloor : Base, IRevitHasFamilyAndType, IRevitHasParameters, IRevitHasTypeParameters, IFloor
  {
    public ICurve outline { get; set; }

    [SchemaOptional]
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [SchemaOptional]
    public bool structural { get; set; }

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
