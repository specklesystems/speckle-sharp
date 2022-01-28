using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.TeklaStructures
{
    public class TeklaContourPlate : Base, IDisplayMesh
    {
        public string name { get; set; }
        public string profile { get; set; }
        public string material { get; set; }
        public string finish { get; set; }
        public string classNumber { get; set; }
        public Base userProperties { get; set; }
        List<Point> controlPoints { get; set; }

        [DetachProperty]
        public Mesh displayMesh { get; set; }

        public string units { get; set; }

        [SchemaInfo("ContourPlate", "Creates a TeklaStructures contour plate.", "Tekla", "Structure")]
        public TeklaContourPlate() { }
    }
}
