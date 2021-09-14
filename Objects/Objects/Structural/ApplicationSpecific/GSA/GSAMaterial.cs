using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.GSA.Materials
{
    public class GSAMaterial : Material
    {
        public int nativeId { get; set; }
        public double localElementSize { get; set; }
        public string colour { get; set; }
        public GSAMaterial() { }
    }
}
