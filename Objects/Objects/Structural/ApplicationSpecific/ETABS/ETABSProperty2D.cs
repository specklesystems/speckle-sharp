using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.ETABS.Analysis;

namespace Objects.Structural.ETABS.Properties
{
    public class ETABSProperty2D : Property2D
    {
        public ETABSPropertyType2D type2D;

        public ETABSProperty2D() { }
    }
}
