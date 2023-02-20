using Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Geometry
{
    public class Axis : Base
    {
        public string name { get; set; } 
        public AxisType axisType { get; set; }
        public Plane definition { get; set; }
        public Axis() { }

        [SchemaInfo("Axis", "Creates a Speckle structural axis (a user-defined axis)", "Structural", "Geometry")]
        public Axis(string name, AxisType axisType = AxisType.Cartesian, Plane definition = null)
        {
            this.name = name;
            this.axisType = axisType;
            this.definition = definition;
        }
    }
}

