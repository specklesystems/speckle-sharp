using Objects.Geometry;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
    public class Polyline : Objects.Geometry.Polyline
    {
        public string name { get; set; }
        public int nativeId { get; set; }
        public string colour { get; set; }
        public GridPlane gridPlane { get; set; }
        public Polyline() { }
        public Polyline(string name, int nativeId, string colour, GridPlane gridPlane)
        {
            this.name = name;
            this.nativeId = nativeId;
            this.colour = colour;
            this.gridPlane = gridPlane;
        }
    }
}
