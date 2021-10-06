using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.Properties
{
    public class SectionProperties
    {
        public string name { get; set; }
        public double area { get; set; }
        public double Iy { get; set; } // seccond moment of area about y-axis
        public double Iz { get; set; } // seccond moment of area about z-axis
        public double J { get; set; } // st. venant torsional constant 
        public double Sy { get; set; } // section modulus about y-axis
        public double Sz { get; set; } // section modulus about z-axis
        public SectionProperties() { }

        [SchemaInfo("SectionProperties", "Creates Speckle structural section properties", "Structural", "Section Properties")]
        public SectionProperties(string name, double area, double Iy, double Iz, double J)
        {
            this.name = name;
            this.area = area;
            this.Iy = Iy;
            this.Iz = Iz;
            this.J = J;
        }
    }

    public class SteelSectionProperties : SectionProperties
    {
        public double Sely { get; set; } // elastic section modulus about y-axis
        public double Selz { get; set; } // elastic section modulus about z-axis
        public double Sply { get; set; } // plastic section modulus about y-axis
        public double Splz { get; set; } // plastic section modulus about z-axis
        public double C { get; set; } // warping constant 
        public double ry { get; set; } // radius of gyration about y-axis
        public double rz { get; set; } // radius of gyration about z-axis
        public SteelSectionProperties() { }

        [SchemaInfo("SteelSectionProperties", "Creates Speckle structural steel section properties", "Structural", "Section Properties")]
        public SteelSectionProperties(string name, double area, double Iy, double Iz, double J, double Sely, double Selz, double Sply, double Splz, double C, double ry, double rz) : base(name, area, Iy, Iz, J)
        {
            this.C = C;
            this.Sely = Sely;
            this.Selz = Selz;
            this.Sply = Sply;
            this.Splz = Splz;
            this.ry = ry;
            this.rz = rz;
        }
    }

    public class ConcreteSectionProperties : SectionProperties
    {
        public ConcreteSectionProperties() { }

        [SchemaInfo("SteelSectionProperties", "Creates Speckle structural steel section properties", "Structural", "Section Properties")]
        public ConcreteSectionProperties(string name, double area, double Iy, double Iz, double J) : base(name, area, Iy, Iz, J)
        {
            // todo: implement
        }
    }
}
