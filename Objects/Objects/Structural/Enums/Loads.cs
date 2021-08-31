using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Loading
{
    public enum LoadType
    {
        None,
        Dead,
        Soil,
        Live,
        LiveRoof,
        Wind,
        Snow,
        Rain,
        Thermal,
        Notional,
        Prestress,
        Equivalent,
        Accidental,
        SeismicRSA,
        SeismicAccTorsion,
        SeismicStatic
    }

    public enum ActionType
    {
        None,
        Permanent,
        Variable,
        Accidental
    }

    public enum BeamLoadType
    {
        Point,
        Uniform,
        Linear,
        Patch,
        TriLinear
    }

    public enum AreaLoadType
    {
        Constant,
        Variable,
        Point
    }

    public enum LoadDirection
    {
        X,
        Y,
        Z,
        XX,
        YY,
        ZZ
    }

    public enum CombinationType
    {
        LinearAdd,
        Envelope,
        AbsoluteAdd,
        SRSS,
        RangeAdd // what's this?
    }
}

