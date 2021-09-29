using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.GSA.Geometry;

namespace Objects.Structural.GSA.Loading
{
    public class GSALoadGridLine : GSALoadGrid
    {
        public Polyline polyline { get; set; }
        public bool isProjected { get; set; }
        public List<double> values { get; set; }
        public GSALoadGridLine() { }

        public GSALoadGridLine(int nativeId, GSAGridSurface gridSurface, Axis loadAxis, LoadDirection2D direction, Polyline polyline, bool isProjected, List<double> values)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadCase = loadCase;
            this.gridSurface = gridSurface;
            this.loadAxis = loadAxis;
            this.direction = direction;
            this.polyline = polyline;
            this.isProjected = isProjected;
            this.values = values;
        }
    }





}
