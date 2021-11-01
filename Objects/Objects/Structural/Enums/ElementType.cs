using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Geometry
{
    public enum ElementType1D
    {
        Beam,
        Brace,
        Bar,
        Column,
        Rod,
        Spring,
        Tie,
        Strut,
        Link,
        Damper,
        Cable,
        Spacer,
        Other,
        Null
    }

    public enum ElementType2D
    {
        Quad4,
        Quad8,
        Triangle3,
        Triangle6
    }

    public enum ElementType3D
    {
        Brick8,
        Wedge6,
        Pyramid5,
        Tetra4
    }
}

