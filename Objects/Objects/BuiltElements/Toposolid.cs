using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class SpeckleToposolid : Base, IDisplayValue<List<Mesh>>
  {
    public SpeckleToposolid() { }

    [SchemaInfo("SpeckleToposolid", "Creates a Speckle Toposolid", "BIM", "Architecture")]
    public SpeckleToposolid(
      List<ICurve[]> profiles = null,
      List<Point> topPlanePoints = null,
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

    public string units { get; set; }

    [DetachProperty] public List<Mesh> displayValue { get; set; }
    
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public bool structural { get; set; }
    public double slope { get; set; }
    public Line slopeDirection { get; set; }
  }
}
