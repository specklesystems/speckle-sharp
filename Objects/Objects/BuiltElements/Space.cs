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
        public Point basePoint { get; set; }
        public Level level { get; set; }
        public double baseOffset { get; set; } = 0;
        public Level upperLimit { get; set; }
        public double limitOffset { get; set; } = 0; 
        public ICurve boundary { get; set; }
        public string spaceType { get; set; }
        public string zoneId { get; set; } 

        // additional properties to add: also inclue space separation lines here? Phase? Associated Room? Zone object instead of id?

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

        [SchemaInfo("Space with upper limit and offset parameters", "Creates a Speckle space with the specified upper limit and offsets", "BIM", "MEP")]
        public Space(string name, string number, [SchemaMainParam] Point basePoint, Level level, Level upperLimit, double limitOffset, double baseOffset)
        {
            this.name = name;
            this.number = number;
            this.basePoint = basePoint;
            this.level = level;
            this.upperLimit = upperLimit;
            this.limitOffset = limitOffset;
            this.baseOffset = baseOffset;
        }
    }
}
