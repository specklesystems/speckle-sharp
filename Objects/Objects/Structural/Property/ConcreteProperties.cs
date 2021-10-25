using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Structural.Properties
{
    public class ConcreteProperties
    {
        public double cover { get; set; } // the concrete cover
        public List<ReinforcementBar> longitudinalBars { get; set; } // the longitudinal reinforcement bars of the cross-section
        public List<Links> links { get; set; } // the shear or torsion links of the cross-section
        public ConcreteProperties() { }
        public ConcreteProperties(double cover)
        {
            this.cover = cover;
        }
    }

    public class ReinforcementBar
    {
        double localY { get; set; } // local y-coordinate
        double localZ { get; set; } // local z-coordinate
        double diameter { get; set; } // diameter of bar
        string unit { get; set; }
        BarArrangement arrangement { get; set; }

        public ReinforcementBar() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="y">Local y-coordinate</param>
        /// <param name="z">Local z-coordinate</param>
        /// <param name="diameter">Diameter of bar</param>
        /// <param name="unit">Bar diameter unit</param>
        /// <param name="arrangement">Bar arrangement</param>
        public ReinforcementBar(double localY, double localZ, double diameter, string unit, BarArrangement arrangement = BarArrangement.Single)
        {
            this.localY = localY;
            this.localZ = localZ;
            this.diameter = diameter;
            this.unit = unit;
            this.arrangement = arrangement;
        }
    }

    public class ReinforcementBundle
    {
        List<ReinforcementBar> reinforcementBars { get; set; }

        public ReinforcementBundle() { }

        public ReinforcementBundle(List<ReinforcementBar> reinforcementBars)
        {
            this.reinforcementBars = reinforcementBars;
        }
    }

    // is a differentiation between shear and torsion links necessary?
    public class Links
    {
        double diameter { get; set; } // diameter of bar
        double longitudinalSpacing { get; set; } // the longitudinal spacing of the links
        double transverseSpacing { get; set; } // the transverse spacing of the links
        public BaseReferencePoint referencePoint { get; set; }
        public double offsetY { get; set; } = 0; // offset from reference point
        public double offsetZ { get; set; } = 0; // offset from reference point

        public Links() { }

        public Links(double diameter, double longitudinalSpacing)
        {
            this.diameter = diameter;
            this.longitudinalSpacing = longitudinalSpacing;
        }
    }

    public enum BarArrangement
    {
        Single, // or Individual?
        Bundled
    }
}
