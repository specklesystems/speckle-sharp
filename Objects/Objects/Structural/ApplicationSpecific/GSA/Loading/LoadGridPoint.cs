using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
    public class LoadGridPoint : LoadGrid
    {
        public Point position { get; set; }
        public double value { get; set; }
        public LoadGridPoint() { }

        public LoadGridPoint(int nativeId, GridSurface gridSurface, Axis loadAxis, LoadDirection2D direction, Point position, double value)
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
