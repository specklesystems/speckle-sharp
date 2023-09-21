using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit
{
  public class RevitToposolid : Base, IDisplayValue<List<Mesh>>
  {
    public RevitToposolid() { }

    [SchemaInfo("RevitToposolid", "Creates a Revit Toposolid", "BIM", "Architecture")]
    public RevitToposolid(
      List<ICurve[]> profiles,
      List<Point> topPlanePoints,
      [SchemaParamInfo("Any nested elements that this floor might have")]
      List<Base> elements = null
    )
    {
      this.profiles = profiles;
      this.points = topPlanePoints;
      this.elements = elements;
    }

    public List<ICurve[]> profiles { get; set; } = new();

    public List<Point> points { get; set; } = new();

    [DetachProperty] public List<Base> elements { get; set; }

    [DetachProperty] public List<Mesh> displayValue { get; set; }
    
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
  }
}
