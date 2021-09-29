using Objects.Geometry;
using Speckle.Core.Kits;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
    public class GSAPolyline : Polyline
    {
        public string name { get; set; }
        public int nativeId { get; set; }
        public string colour { get; set; }
        public GSAGridPlane gridPlane { get; set; }
        public GSAPolyline() { }

        [SchemaInfo("GSAPolyline", "Creates a Speckle structural polyline for GSA", "GSA", "Geometry")]
        public GSAPolyline(string name, int nativeId, string colour, GSAGridPlane gridPlane)
        {
            this.name = name;
            this.nativeId = nativeId;
            this.colour = colour;
            this.gridPlane = gridPlane;
        }
    }
}
