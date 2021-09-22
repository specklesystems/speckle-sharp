using Speckle.Core.Kits;
using Objects.Structural.Properties;
using Objects.Structural.Materials;

namespace Objects.Structural.GSA.Properties
{
    public class Property2D : Structural.Properties.Property2D
    {
        public int nativeId { get; set; }
        public Material designMaterial { get; set; }
        public double cost { get; set; }
        public double additionalMass { get; set; }
        public string concreteSlabProp { get; set; }
        public string colour { get; set; }
        public Property2D() { }

        [SchemaInfo("GSAProperty2D", "Creates a Speckle structural 2D element property for GSA", "GSA", "Properties")]
        public Property2D(int nativeId, string name, Material material, double thickness)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.material = material;
            this.thickness = thickness;
        }
    }
}
