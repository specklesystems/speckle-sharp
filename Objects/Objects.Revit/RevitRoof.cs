using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaIgnore]

  public class RevitRoof : Base, IRevitHasFamilyAndType, IRoof
  {
    public ICurve outline { get; set; }

    [SchemaOptional]
    public List<ICurve> voids { get; set; } = new List<ICurve>();

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
  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }
  }

  public class RevitFootprintRoof : RevitRoof
  {
    public RevitLevel cutOffLevel { get; set; }
  }
}
