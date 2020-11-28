using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using static Objects.BuiltElements.Revit.RevitUtils;

namespace Objects.BuiltElements.Revit
{
  [SchemaDescription("A DirectShape element by mesh")]
  public class DirectShape : Base
  {
    public string type { get; set; }

    public RevitCategory category { get; set; }

    public Mesh baseGeometry { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

}
