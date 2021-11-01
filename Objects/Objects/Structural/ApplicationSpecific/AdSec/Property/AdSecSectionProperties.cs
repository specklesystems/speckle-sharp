using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.AdSec.Properties
{
    public class AdSecSectionProperties : Base
    {
        public double area { get; set; }
        public double Iy { get; set; } // seccond moment of area about y-axis
        public double Iz { get; set; } // seccond moment of area about z-axis
        public double J { get; set; } // st. venant torsional constant 
        public double Sy { get; set; } // elastic section modulus about y-axis
        public double Sz { get; set; } // elastic section modulus about z-axis
        public AdSecSectionProperties() { }

        [SchemaInfo("AdSecSectionProperties", "Creates Speckle structural section properties for AdSec", "AdSec", "Section Properties")]
        public AdSecSectionProperties(double area, double Iy, double Iz, double J, double Sz, double Sy)
        {
            this.area = area;
            this.Iy = Iy;
            this.Iz = Iz;
            this.J = J;
            this.Sy = Sy;
            this.Sz = Sz;
        }
    }
}
