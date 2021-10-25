using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.AdSec.Properties
{
    public class AdSecSectionPropertiesSteel : AdSecSectionProperties
    {
        public double Sply { get; set; } // plastic section modulus about y-axis
        public double Splz { get; set; } // plastic section modulus about z-axis
        public double C { get; set; } // warping constant 
        public double ry { get; set; } // radius of gyration about y-axis
        public double rz { get; set; } // radius of gyration about z-axis
        public double y0 { get; set; } // distance from centroid to shear center in y-axis direction
        public double z0 { get; set; } // distance from centroid to shear center in z-axis direction
        public AdSecSectionPropertiesSteel() { }

        [SchemaInfo("AdSecSectionPropertiesSteel", "Creates Speckle structural steel section properties", "AdSec", "Section Properties")]
        public AdSecSectionPropertiesSteel(double area, double Iy, double Iz, double J, double Sely, double Selz, double Sply, double Splz, double C, double ry, double rz, double y0 = 0.0, double z0 = 0.0) : base(area, Iy, Iz, J, Sely, Selz)
        {
            this.C = C;
            this.Sply = Sply;
            this.Splz = Splz;
            this.ry = ry;
            this.rz = rz;
            this.y0 = y0;
            this.z0 = z0;
        }
    }
}
