using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.GSA.Geometry //GSA.Geometry?
{
    public class GSAMember2D : Element2D
    {
        public int nativeId { get; set; }
        public int group { get; set; }
        public string colour { get; set; }
        public string action { get; set; }
        public bool isDummy { get; set; }
        public bool intersectsWithOthers { get; set; }
        public double targetMeshSize { get; set; }
        //public Result results { get; set; }
        public GSAMember2D() { }

        [SchemaInfo("GSAMember2D", "Creates a Speckle structural 2D member for GSA")]
        public GSAMember2D(Mesh baseMesh, Property2D property, ElementType2D type, double offset = 0, double orientationAngle = 0)
        {
            this.baseMesh = baseMesh;
            this.property = property;
            this.type = type;
            this.offset = offset;
            this.orientationAngle = orientationAngle;
        }
    }
}
