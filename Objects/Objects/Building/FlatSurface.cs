using System;
using System.Collections.Generic;
using System.Text;
using Objects.Building.enums;
using Objects.Organization;
using Objects.Definitions;

namespace Objects.Building
{
  public class FlatSurface : CurveBasedElement
  {
  public flatSurfaceType Type{ get; set; }

  public double area { get; set; }

  public Level level { get; set; }
  public double thickness { get; set; }
    // to implement source app parameters interface from claire
  }
}
