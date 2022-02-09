using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements.TeklaStructures
{
  public enum TeklaBeamType
  {
    Beam,
    PolyBeam,
    SpiralBeam
  }

  public enum TeklaChamferType
  {
    none,
    line,
    rounding,
    arc,
    arc_point,
    square,
    square_parallel,
    line_and_arc
  }
}
