using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.BuiltElements;

namespace Objects.Structural.GSA.Geometry
{
    public class GSAGridLine : GridLine
    {
        public int nativeId { get; set; }
        public GSAGridLine() { }

        public GSAGridLine(int nativeId, string name, ICurve line)
        {            
            this.nativeId = nativeId;
            this.label = name;
            this.baseLine = line;
        }
    }

    public class GSAGridPlane : Storey
    {
        public int nativeId { get; set; }
        public Axis axis { get; set; }
        public GSAGridPlane() { }

        public GSAGridPlane(int nativeId, string name, Axis axis, double elevation)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.axis = axis;
            this.elevation = elevation; // the height of the grid plane above the origin (of the specified axis)
        }
    }

    public class GSAStorey : Storey
    {
        public int nativeId { get; set; }
        public Axis axis { get; set; }
        public double toleranceBelow { get; set; }
        public double toleranceAbove { get; set; }
        public GSAStorey() { }

        public GSAStorey(int nativeId, string name, Axis axis, double elevation, double toleranceBelow, double toleranceAbove)
        {            
            this.nativeId = nativeId;
            this.name = name;
            this.axis = axis;
            this.elevation = elevation;
            this.toleranceBelow = toleranceBelow;
            this.toleranceAbove = toleranceAbove;
        }
    }

    public class GSAGridSurface : Base
    {
        public string name { get; set; }
        public int nativeId { get; set; }
        public GSAGridPlane gridPlane { get; set; }
        public double tolerance { get; set; }
        public double spanDirection { get; set; }
        public LoadExpansion loadExpansion { get; set; }
        public GridSurfaceSpanType span { get; set; }

        [DetachProperty]
        [Chunkable(5000)]
        public List<Base> elements { get; set; }
        public GSAGridSurface() { }

        public GSAGridSurface(string name, int nativeId, GSAGridPlane gridPlane, double tolerance, double spanDirection, LoadExpansion loadExpansion, GridSurfaceSpanType span, List<Base> elements)
        {
            this.name = name;
            this.nativeId = nativeId;
            this.gridPlane = gridPlane;
            this.tolerance = tolerance;
            this.spanDirection = spanDirection;
            this.loadExpansion = loadExpansion;
            this.span = span;
            this.elements = elements;
        }
    }


    public enum GridSurfaceSpanType
    {
        NotSet = 0,
        OneWay,
        TwoWay
    }

    public enum LoadExpansion
    {
        NotSet = 0,
        Legacy = 1,
        PlaneAspect = 2,
        PlaneSmooth = 3,
        PlaneCorner = 4
    }
}
