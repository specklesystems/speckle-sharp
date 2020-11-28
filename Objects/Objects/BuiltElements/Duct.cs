using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Duct : Base
  {
    public Line baseLine { get; set; }

    [SchemaOptional]
    public double width { get; set; }

    [SchemaOptional]
    public double height { get; set; }

    [SchemaOptional]
    public double diameter { get; set; }

    [SchemaOptional]
    public double length { get; set; }

    [SchemaOptional]
    public double velocity { get; set; }

    public Duct() { }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitDuct : Duct
  {
    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public string systemName { get; set; }

    [SchemaOptional]
    public string systemType { get; set; }

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
