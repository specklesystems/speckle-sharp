using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Geometry;

public class GSAElement3D : Element3D
{
  public GSAElement3D() { }

  [SchemaInfo("GSAElement3D", "Creates a Speckle structural 3D element for GSA", "GSA", "Geometry")]
  public GSAElement3D(
    int nativeId,
    Mesh baseMesh,
    Property3D property,
    ElementType3D type,
    string name = null,
    double orientationAngle = 0,
    int group = 0,
    string colour = "NO_RGB",
    bool isDummy = false
  )
  {
    this.nativeId = nativeId;
    this.baseMesh = baseMesh;
    this.property = property;
    this.type = type;
    this.name = name;
    this.orientationAngle = orientationAngle;
    this.group = group;
    this.colour = colour;
    this.isDummy = isDummy;
  }

  public int nativeId { get; set; }
  public int group { get; set; }
  public string colour { get; set; }
  public bool isDummy { get; set; }
}
