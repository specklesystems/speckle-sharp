using Objects.Structural.Geometry;

namespace Objects.Structural.GSA.Geometry
{
    public class Storey : Structural.Geometry.Storey
    {
        public int nativeId { get; set; }
        public Axis axis { get; set; }
        public double toleranceBelow { get; set; }
        public double toleranceAbove { get; set; }
        public Storey() { }

        public Storey(int nativeId, string name, Axis axis, double elevation, double toleranceBelow, double toleranceAbove)
        {            
            this.nativeId = nativeId;
            this.name = name;
            this.axis = axis;
            this.elevation = elevation;
            this.toleranceBelow = toleranceBelow;
            this.toleranceAbove = toleranceAbove;
        }
    }



}
