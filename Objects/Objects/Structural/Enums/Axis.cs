using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural
{
  public enum AxisType
  {
    Cartesian,
    Cylindrical,
    Spherical
  }

  public enum LoadAxisType
  {
    Global,
    Local, // local element axes
    DeformedLocal // element local axis that is embedded in the element as it deforms 
  }
}

