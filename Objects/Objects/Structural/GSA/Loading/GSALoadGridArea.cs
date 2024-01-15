using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.GSA.Loading;

public class GSALoadGridArea : GSALoadGrid
{
  public GSALoadGridArea() { }

  public GSALoadGridArea(
    int nativeId,
    GSAGridSurface gridSurface,
    Axis loadAxis,
    LoadDirection2D direction,
    Polyline polyline,
    bool isProjected,
    double value
  )
  {
    this.nativeId = nativeId;
    this.gridSurface = gridSurface;
    this.loadAxis = loadAxis;
    this.direction = direction;
    this.polyline = polyline;
    this.isProjected = isProjected;
    this.value = value;
  }

  public Polyline polyline { get; set; }
  public bool isProjected { get; set; }
  public double value { get; set; }
}
