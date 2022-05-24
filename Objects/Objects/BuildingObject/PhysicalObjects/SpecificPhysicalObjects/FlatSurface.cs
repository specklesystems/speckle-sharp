using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.enums;
using Objects.ProjectOrganization;

namespace Objects.BuildingObject.PhysicalObjects
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
