using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
  public class GSALoadGridPoint : GSALoadGrid
  {
    public Point position { get; set; }
    public double value { get; set; }
    public GSALoadGridPoint() { }

    public GSALoadGridPoint(int nativeId, GSAGridSurface gridSurface, Axis loadAxis, LoadDirection2D direction, Point position, double value)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.loadCase = loadCase;
      this.gridSurface = gridSurface;
      this.loadAxis = loadAxis;
      this.direction = direction;
      this.position = position;
      this.value = value;
    }
  }





}
