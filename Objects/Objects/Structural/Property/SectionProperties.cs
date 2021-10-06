using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
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
        public double Sy { get; set; } // elastic section modulus about y-axis
        public double Sz { get; set; } // elastic section modulus about z-axis
        public SectionProperties() { }

        [SchemaInfo("SectionProperties", "Creates Speckle structural section properties", "Structural", "Section Properties")]
        public SectionProperties(string name, double area, double Iy, double Iz, double J, double Sz, double Sy)
        {
            this.name = name;
            this.area = area;
            this.Iy = Iy;
            this.Iz = Iz;
            this.J = J;
            this.Sy = Sy;
            this.Sz = Sz;
        }
    }

    public class SteelSectionProperties : SectionProperties
    {
        public double Sply { get; set; } // plastic section modulus about y-axis
        public double Splz { get; set; } // plastic section modulus about z-axis
        public double C { get; set; } // warping constant 
        public double ry { get; set; } // radius of gyration about y-axis
        public double rz { get; set; } // radius of gyration about z-axis
        public SteelSectionProperties() { }

        [SchemaInfo("SteelSectionProperties", "Creates Speckle structural steel section properties", "Structural", "Section Properties")]
        public SteelSectionProperties(string name, double area, double Iy, double Iz, double J, double Sely, double Selz, double Sply, double Splz, double C, double ry, double rz) : base(name, area, Iy, Iz, J, Sely, Selz)
        {
            this.C = C;
            this.Sply = Sply;
            this.Splz = Splz;
            this.ry = ry;
            this.rz = rz;
        }
    }

    public class ConcreteSectionProperties : SectionProperties
    {
        public List<ReinforcementBar> rebars { get; set; }
        public ConcreteSectionProperties() { }

        [SchemaInfo("SteelSectionProperties", "Creates Speckle structural steel section properties", "Structural", "Section Properties")]
        public ConcreteSectionProperties(string name, double area, double Iy, double Iz, double J, double Sz, double Sy) : base(name, area, Iy, Iz, J, Sy, Sz)
        {
            // todo: implement
        }
    }

    public class ReinforcementBar : Base
    {
        double y { get; set; } // local y-coordinate
        double z { get; set; } // local z-coordinate
        double diameter { get; set; } // diameter of bar or equivalent diameter
        string units { get; set; }
        BarArrangement arrangement { get; set; }

        public ReinforcementBar() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="y">Local y-coordinate</param>
        /// <param name="z"></param>
        /// <param name="diameter">Diameter of bar or equivalent diameter</param>
        /// <param name="units">The unit of the bar diameter</param>
        /// <param name="arrangement">Arrangement of bars</param>
        public ReinforcementBar(double y, double z, double diameter, string units, BarArrangement arrangement = BarArrangement.Individual)
        {
            this.y = y;
            this.z = z;
            this.diameter = diameter;
            this.units = units;
            this.arrangement = arrangement;
        }
    }

    public enum BarArrangement
    {
        Individual,
        Bundled
    }
}
