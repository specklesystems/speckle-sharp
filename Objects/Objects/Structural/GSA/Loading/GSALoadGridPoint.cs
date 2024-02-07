using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.GSA.Loading;

public class GSALoadGridPoint : GSALoadGrid
{
  public GSALoadGridPoint() { }

  public GSALoadGridPoint(
    int nativeId,
    GSAGridSurface gridSurface,
    Axis loadAxis,
    LoadDirection2D direction,
    Point position,
    double value
  )
  {
    this.nativeId = nativeId;
    this.gridSurface = gridSurface;
    this.loadAxis = loadAxis;
    this.direction = direction;
    this.position = position;
    this.value = value;
  }

  public Point position { get; set; }
  public double value { get; set; }
}
