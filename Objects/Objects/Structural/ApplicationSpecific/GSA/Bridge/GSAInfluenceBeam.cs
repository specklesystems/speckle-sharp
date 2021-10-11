using Speckle.Core.Models;
using Speckle.Core.Kits;
using Objects.Structural.Loading;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Bridge
{
    public class GSAInfluenceBeam : GSAInfluence
    {
        [DetachProperty]
        public GSAElement1D element { get; set; }
        public double position { get; set; }
        public GSAInfluenceBeam() { }

        [SchemaInfo("GSAInfluenceBeam", "Creates a Speckle structural beam influence effect for GSA (for an influence analysis)", "GSA", "Bridge")]
        public GSAInfluenceBeam(int nativeId, string name, double factor, InfluenceType type, LoadDirection direction, GSAElement1D element, double position)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.factor = factor;
            this.type = type;
            this.direction = direction;
            this.element = element;
            this.position = position;
        }
    }
}
