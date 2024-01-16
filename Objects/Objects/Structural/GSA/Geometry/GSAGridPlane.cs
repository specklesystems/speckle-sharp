using Objects.Structural.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Geometry;

public class GSAGridPlane : Storey
{
  public GSAGridPlane() { }

  [SchemaInfo("GSAGridPlane", "Creates a Speckle structural grid plane for GSA", "GSA", "Geometry")]
  public GSAGridPlane(int nativeId, string name, Axis axis, double elevation)
  {
    this.nativeId = nativeId;
    this.name = name;
    this.axis = axis;
    this.elevation = elevation; // the height of the grid plane above the origin (of the specified axis)
  }

  public int nativeId { get; set; }

  [DetachProperty]
  public Axis axis { get; set; }

  public double? toleranceBelow { get; set; }
  public double? toleranceAbove { get; set; }
}
