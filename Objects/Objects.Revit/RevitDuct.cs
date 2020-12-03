using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitDuct : Base, IRevitHasFamilyAndType, IDuct
  {
    public double width { get; set; }

    public double height { get; set; }

    public double diameter { get; set; }

    public double length { get; set; }

    public double velocity { get; set; }

    public string systemName { get; set; }

    public string systemType { get; set; }

    public Line baseLine { get; set; }

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
