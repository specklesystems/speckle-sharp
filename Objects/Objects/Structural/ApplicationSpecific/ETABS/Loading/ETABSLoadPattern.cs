using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Structural.ApplicationSpecific.ETABS.Loading
{
    public class ETABSLoadPattern : LoadCase
    {
        public double SelfWeightMultiplier { get; set; } = 0;
    }
}
