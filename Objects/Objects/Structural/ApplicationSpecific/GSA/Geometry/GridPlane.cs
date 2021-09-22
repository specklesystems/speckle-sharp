using Objects.Structural.Geometry;

namespace Objects.Structural.GSA.Geometry
{
    public class GridPlane : Structural.Geometry.Storey
    {
        public int nativeId { get; set; }
        public Axis axis { get; set; }
        public GridPlane() { }

        public GridPlane(int nativeId, string name, Axis axis, double elevation)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.axis = axis;
            this.elevation = elevation; // the height of the grid plane above the origin (of the specified axis)
        }
    }
}
