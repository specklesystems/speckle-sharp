using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;


namespace Objects.BuiltElements.TeklaStructures
{
    public class TeklaContourPlate : Area
    {
        public SectionProfile profile { get; set; }
        public Material material { get; set; }
        public string finish { get; set; }
        public string classNumber { get; set; }
        //public Base userProperties { get; set; }
        //public List<TeklaContourPoint> contourPoints { get; set; }

        [DetachProperty]
        public Mesh displayMesh { get; set; }

        public string units { get; set; }

        [SchemaInfo("ContourPlate", "Creates a TeklaStructures contour plate.", "Tekla", "Structure")]
        public TeklaContourPlate() { }
    }
    public class TeklaContourPoint : Point
    {
        public TeklaChamferType chamferType { get; set; }
        public double xDim { get; set; }
        public double yDim { get; set; }
        public double dz1 { get; set; }
        public double dz2 { get; set; }

        public TeklaContourPoint() { }
        public TeklaContourPoint(Point point) { }
    }
}
