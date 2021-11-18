using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
    public abstract class GSALoadGrid : Load
    {
        public int nativeId { get; set; }
        public GSAGridSurface gridSurface { get; set; }
        public Axis loadAxis { get; set; }
        public LoadDirection2D direction { get; set; }
        public GSALoadGrid() { }
        public GSALoadGrid(int nativeId, GSAGridSurface gridSurface, Axis loadAxis, LoadDirection2D direction)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadCase = loadCase;
            this.gridSurface = gridSurface;
            this.loadAxis = loadAxis;
            this.direction = direction;
        }
    }





}
