using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.Properties.Profiles
{
    public class SectionProfile : Base //section profile description
    {
        public string name { get; set; }        
        public ShapeType shapeType { get; set; }
        public double area { get; set; }
        public double Iyy { get; set; }
        public double Izz { get; set; }
        public double J { get; set; }
        public double Ky { get; set; }
        public double Kz { get; set; }
        public double weight { get; set; } //section weight, ex. kg/m
        public string units { get; set; }
        public SectionProfile() { }

        public class Rectangular : SectionProfile
        {            
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; } // tw 
            public double flangeThickness { get; set; } // tf

            public Rectangular() { }

            [SchemaInfo("Rectangular", "Creates a Speckle structural rectangular section profile", "Structural", "Section Profile")]
            public Rectangular(string name, double depth, double width, double webThickness = 0, double flangeThickness = 0)
            {
                this.name = name;                
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.Rectangular;
            }
        }

        public class Circular : SectionProfile
        {
            public double radius { get; set; }
            public double wallThickness { get; set; }

            public Circular() { }

            [SchemaInfo("Circular", "Creates a Speckle structural circular section profile", "Structural", "Section Profile")]
            public Circular(string name, double radius, double wallThickness = 0)
            {
                this.name = name;
                this.radius = radius;
                this.wallThickness = wallThickness;
                this.shapeType = ShapeType.Circular;
            }
        }

        public class ISection : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public ISection() { }

            [SchemaInfo("ISection", "Creates a Speckle structural I section profile", "Structural", "Section Profile")]
            public ISection(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.I;
            }
        }

        public class Tee : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public Tee() { }

            [SchemaInfo("Tee", "Creates a Speckle structural Tee section profile", "Structural", "Section Profile")]
            public Tee(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.Tee;
            }
        }

        public class Angle : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public Angle() { }

            [SchemaInfo("Angle", "Creates a Speckle structural angle section profile", "Structural", "Section Profile")]
            public Angle(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.Angle;
            }
        }

        public class Channel : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public Channel() { }

            [SchemaInfo("Channel", "Creates a Speckle structural channel section profile", "Structural", "Section Profile")]
            public Channel(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.Channel;
            }
        }

        public class Perimeter : SectionProfile
        {
            public ICurve outline { get; set; }
            public List<ICurve> voids { get; set; } = new List<ICurve>();

            public Perimeter() { }

            [SchemaInfo("Perimeter", "Creates a Speckle structural section profile defined by a perimeter curve and, if applicable, a list of void curves", "Structural", "Section Profile")]
            public Perimeter(string name, ICurve outline, List<ICurve> voids = null)
            {
                this.name = name;
                this.outline = outline;
                this.voids = voids;
            }
        }

        public class Catalogue : SectionProfile
        {
            public string description { get; set; } // a description string for a catalogue section, per a to be defined convention for industry-typical, commonly manufactured sections - SAF Formcodes, Oasys profiles?
            public string catalogueName { get; set; } // ex. AISC, could be enum value
            public string sectionType { get; set; } // ex. W shapes, could be enum value
            public string sectionName { get; set; } // ex. W44x335, could be enum value

            public Catalogue() { }

            [SchemaInfo("Catalogue (by description)", "Creates a Speckle structural section profile based on a catalogue section description", "Structural", "Section Profile")]
            public Catalogue(string description)
            {
                this.description = description;
            }

            [SchemaInfo("Catalogue", "Creates a Speckle structural section profile", "Structural", "Section Profile")]
            public Catalogue(string name, string catalogueName, string sectionType, string sectionName)
            {
                this.name = name;
                this.catalogueName = catalogueName;
                this.sectionType = sectionType;
                this.sectionName = sectionName;
            }
        }

        public class Explicit : SectionProfile
        {
            public Explicit() { }

            [SchemaInfo("Explicit", "Creates a Speckle structural section profile based on explicitly defining geometric properties", "Structural", "Section Profile")]
            public Explicit(string name, double area, double Iyy, double Izz, double J, double Ky, double Kz)
            {
                this.name = name;
                this.area = area;
                this.Iyy = Iyy;
                this.Izz = Izz;
                this.J = J;
                this.Ky = Ky;
                this.Kz = Kz;
                this.shapeType = ShapeType.Explicit;
            }
        }

        public class Cruciform : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public Cruciform() { }

            [SchemaInfo("Cruciform", "Creates a Speckle structural cruciform section profile", "Structural", "Section Profile")]
            public Cruciform(string name, double depth, double width, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.Cruciform;
            }
        }

        public class Ellipse : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double wallThickness { get; set; }

            public Ellipse() { }

            [SchemaInfo("Ellipse", "Creates a Speckle structural ellipse section profile", "Structural", "Section Profile")]
            public Ellipse(string name, double depth, double width, double wallThickness = 0)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.wallThickness = wallThickness;
                this.shapeType = ShapeType.Ellipse;
            }
        }

        public class C : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double wallThickness { get; set; }
            public double lipDepth { get; set; }

            public C() { }

            [SchemaInfo("General C", "Creates a Speckle structural C section profile", "Structural", "Section Profile")]
            public C(string name, double depth, double width, double wallThickness, double lipDepth)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.wallThickness = wallThickness;
                this.lipDepth = lipDepth;
                this.shapeType = ShapeType.C;
            }
        }

        public class Z : SectionProfile
        {
            public double depth { get; set; }
            public double topFlangeWidth { get; set; }
            public double botFlangeWidth { get; set; }
            public double wallThickness { get; set; }
            public double topLipDepth { get; set; }
            public double botLipDepth { get; set; }

            public Z() { }

            [SchemaInfo("General Z", "Creates a Speckle structural Z section profile", "Structural", "Section Profile")]
            public Z(string name, double depth, double topFlangeWidth, double botFlangeWidth, double wallThickness, double topLipDepth, double botLipDepth)
            {
                this.name = name;
                this.depth = depth;
                this.topFlangeWidth = topFlangeWidth;
                this.botFlangeWidth = botFlangeWidth;
                this.wallThickness = wallThickness;
                this.topLipDepth = topLipDepth;
                this.botLipDepth = botLipDepth;
                this.shapeType = ShapeType.Z;
            }
        }

        public class IAssymetric : SectionProfile
        {
            public double depth { get; set; }
            public double topFlangeWidth { get; set; }
            public double botFlangeWidth { get; set; }
            public double wallThickness { get; set; }
            public double topFlangeThickness { get; set; }
            public double botFlangeThickness { get; set; }

            public IAssymetric() { }

            [SchemaInfo("Assymetric I", "Creates a Speckle structural assymetric I section profile", "Structural", "Section Profile")]
            public IAssymetric(string name, double depth, double topFlangeWidth, double botFlangeWidth, double wallThickness, double topFlangeThickness, double botFlangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.topFlangeWidth = topFlangeWidth;
                this.botFlangeWidth = botFlangeWidth;
                this.wallThickness = wallThickness;
                this.topFlangeThickness = topFlangeThickness;
                this.botFlangeThickness = botFlangeThickness;
                this.shapeType = ShapeType.IAssymetric;
            }
        }

        public class RectoEllipse : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double depthFlat { get; set; }
            public double widthFlat { get; set; }

            public RectoEllipse() { }

            [SchemaInfo("Recto Ellipse", "Creates a Speckle structural rectangular profile with elliptical corners", "Structural", "Section Profile")]
            public RectoEllipse(string name, double depth, double width, double depthFlat, double widthFlat)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.depthFlat = depthFlat;
                this.widthFlat = widthFlat;
                this.shapeType = ShapeType.RectoEllipse;
            }
        }

        public class SecantPile : SectionProfile
        {
            public double diameter { get; set; }
            public double pileCentres { get; set; }
            public int pileCount { get; set; }
            public bool isWall { get; set; }

            public SecantPile() { }

            [SchemaInfo("Secant Pile", "Creates a Speckle structural secant pile section profile", "Structural", "Section Profile")]
            public SecantPile(string name, double diameter, double pileCentres, int pileCount, bool isWall)
            {
                this.name = name;
                this.diameter = diameter;
                this.pileCentres = pileCentres;
                this.pileCount = pileCount;
                this.isWall = isWall;
                this.shapeType = ShapeType.SecantPile;
            }
        }

        public class SheetPile : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }
            public double topFlangeWidth { get; set; }
            public double botFlangeWidth { get; set; }
            public double webThickness { get; set; }
            public double flangeThickness { get; set; }

            public SheetPile() { }

            [SchemaInfo("Sheet Pile", "Creates a Speckle structural sheet pile section profile", "Structural", "Section Profile")]
            public SheetPile(string name, double depth, double width, double topFlangeWidth, double botFlangeWidth, double webThickness, double flangeThickness)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.topFlangeWidth = topFlangeWidth;
                this.botFlangeWidth = botFlangeWidth;
                this.webThickness = webThickness;
                this.flangeThickness = flangeThickness;
                this.shapeType = ShapeType.SheetPile;
            }
        }

        public class Stadium : SectionProfile
        {
            public double depth { get; set; }
            public double width { get; set; }

            public Stadium() { }

            [SchemaInfo("Stadium", "Creates a Speckle structural stadium section profile. It is a profile consisting of a rectangle whose ends are capped off with semicircles", "Structural", "Section Profile")]
            public Stadium(string name, double depth, double width)
            {
                this.name = name;
                this.depth = depth;
                this.width = width;
                this.shapeType = ShapeType.Stadium;
            }
        }

        public class Trapezoid : SectionProfile
        {
            public double depth { get; set; }
            public double topWidth { get; set; }
            public double botWidth { get; set; }

            public Trapezoid() { }

            [SchemaInfo("Trapezoid", "Creates a Speckle structural trapezoidal section profile", "Structural", "Section Profile")]
            public Trapezoid(string name, double depth, double topWidth, double botWidth)
            {
                this.name = name;
                this.depth = depth;
                this.topWidth = topWidth;
                this.botWidth = botWidth;
                this.shapeType = ShapeType.Trapezoid;
            }
        }
    }
}
