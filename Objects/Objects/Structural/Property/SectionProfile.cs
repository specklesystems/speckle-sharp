using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.Properties
{
    public class SectionProfile : Base //section description
    {
        public string name { get; set; }
        public string shapeDescription { get; set; } //not needed anymore?
        public ShapeType shapeType { get; set; }
        public double weight { get; set; } //section weight, ex. kg/m
        public SectionProfile() { }

        [SchemaInfo("SectionProfile", "Creates a Speckle structural 1D element section profile", "Structural", "Properties")]
        public SectionProfile(string shapeDescription)
        {
            this.shapeDescription = shapeDescription;
        }

        public class RectangularProfile : SectionProfile
        {            
            public double depth { get; set; }
            public double width { get; set; }
            public double thickness { get; set; }

            public RectangularProfile() { }

            [SchemaInfo("RectangularProfile", "Creates a Speckle structural rectangular section profile", "Structural", "Properties")]
            public RectangularProfile(string name, double depth, double width, double thickness)
            {
                this.name = name;                
                this.depth = depth;
                this.width = width;
                this.thickness = thickness;
                this.shapeType = ShapeType.Rectangular;
            }
        }

        public class CircularProfile : SectionProfile
        {
            public double radius { get; set; }
            public double thickness { get; set; }

            public CircularProfile() { }

            [SchemaInfo("CircularProfile", "Creates a Speckle structural circular section profile", "Structural", "Properties")]
            public CircularProfile(string name, double radius, double thickness)
            {
                this.name = name;
                this.radius = radius;
                this.thickness = thickness;
                this.shapeType = ShapeType.Circular;
            }
        }

        public class ISectionProfile : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public ISectionProfile() { }

            [SchemaInfo("ISectionProfile", "Creates a Speckle structural I section profile", "Structural", "Properties")]
            public ISectionProfile(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.ISection;
            }
        }

        public class TSectionProfile : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public TSectionProfile() { }

            [SchemaInfo("TSectionProfile", "Creates a Speckle structural T section profile", "Structural", "Properties")]
            public TSectionProfile(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.TSection;
            }
        }

    }


    }
}
