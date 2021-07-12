using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
    public class Space : Base, IHasArea, IHasVolume, IDisplayMesh
    {
        public string name { get; set; }
        public string number { get; set; }
        public double area { get; set; }
        public double volume { get; set; }
        public bool isUnbounded { get; set; }
        public Point basePoint { get; set; }
        public Level level { get; set; }
        public double baseOffset { get; set; } = 0;
        public Level upperLimit { get; set; }
        public double upperOffset { get; set; } = 0;
        public ICurve boundary { get; set; }
        public List<ICurve> separationLines { get; set; } 

        [DetachProperty]
        public Mesh displayMesh { get; set; }
        public Space() { }

        [SchemaInfo("Space", "Creates a Speckle space", "BIM", "MEP")]
        public Space(string name, string number, [SchemaMainParam] Point basePoint, Level level, Level upperLimit)
        {
            this.name = name;
            this.number = number;
            this.basePoint = basePoint;
            this.level = level;
            this.upperLimit = upperLimit;
        }
    }
}

namespace Objects.BuiltElements.Revit
{
    public class RevitSpace : Space
    {
        public string elementId { get; set; }
        public List<Parameter> parameters { get; set; }

        public RevitSpace() { }

        [SchemaInfo("RevitSpace", "Creates a Revit space", "Revit", "MEP")]
        public RevitSpace([SchemaMainParam] Mesh displayMesh, List<Parameter> parameters = null)
        {
            this.displayMesh = displayMesh;
            this.parameters = parameters;
        }
    }
}