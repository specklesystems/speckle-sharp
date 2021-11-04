using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System.Collections.Generic;
using System.Linq;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
    public class GSAPolyline : Polyline
    {
        public string name { get; set; }
        public int nativeId { get; set; }
        public string colour { get; set; }

        [DetachProperty]
        public GSAGridPlane gridPlane { get; set; }
        public GSAPolyline() { }

        [SchemaInfo("GSAPolyline", "Creates a Speckle structural polyline for GSA", "GSA", "Geometry")]
        public GSAPolyline(string name, int nativeId, IEnumerable<double> coordinatesArray, string colour, GSAGridPlane gridPlane)
        {
            this.name = name;
            this.nativeId = nativeId;
            this.value = coordinatesArray.ToList();
            this.colour = colour;
            this.gridPlane = gridPlane;
        }
    }
}
