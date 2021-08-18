using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements.Revit.Curve;

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
        public double limitOffset { get; set; } = 0;
        public ICurve boundary { get; set; }
        public string spaceType { get; set; }

        [DetachProperty]
        public List<SpaceSeparationLine> separationLines { get; set; } 
        //missing Phase? Associated Room? Zone?

        [DetachProperty]
        public Mesh displayMesh { get; set; }
        public string units { get; set; }
        public Space() { }

        [SchemaInfo("Space", "Creates a Speckle space", "BIM", "MEP")]
        public Space(string name, string number, [SchemaMainParam] Point basePoint, Level level)
        {
            this.name = name;
            this.number = number;
            this.basePoint = basePoint;
            this.level = level;
        }

        [SchemaInfo("Space with space separation lines", "Creates a Speckle space with separation lines", "BIM", "MEP")]
        public Space(string name, string number, Point basePoint, [SchemaMainParam] List<SpaceSeparationLine> separationLines, Level level)
        {
            this.name = name;
            this.number = number;
            this.basePoint = basePoint;
            this.separationLines = separationLines;
            this.level = level;
        }
    }
}
