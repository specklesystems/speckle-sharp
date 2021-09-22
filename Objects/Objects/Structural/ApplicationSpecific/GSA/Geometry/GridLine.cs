using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.BuiltElements;

namespace Objects.Structural.GSA.Geometry
{
    public class GridLine : BuiltElements.GridLine
    {
        public int nativeId { get; set; }
        public GridLine() { }

        public GridLine(int nativeId, string name, ICurve line)
        {            
            this.nativeId = nativeId;
            this.label = name;
            this.baseLine = line;
        }
    }
}
