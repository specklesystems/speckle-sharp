using Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Geometry
{
    public enum MemberType1D
    {
        Beam,
        Column,
        Generic1D
    }

    public enum MemberType2D
    {
        Slab,
        Wall,
        Generic2D,
        VoidCutter1D,
        VoidCutter2D
    }
}

