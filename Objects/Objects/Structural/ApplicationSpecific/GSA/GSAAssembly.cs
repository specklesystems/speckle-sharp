using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.GSA.Geometry
{
    public class GSAAssembly : Base
    {
        public int nativeId { get; set; } //equiv to num record of gwa keyword
        public int group { get; set; }
        public string colour { get; set; }
        public string action { get; set; }
        public bool isDummy { get; set; }        
        public GSAAssembly() { }
    }
}
