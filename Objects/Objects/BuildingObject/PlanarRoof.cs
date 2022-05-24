using System;
using System.Collections.Generic;
using System.Text;
using Objects.Definitions;
using Objects.Organization;
using Objects.Geometry;

namespace Objects.BuildingObject
{
  public class PlanarRoof : CurveBasedElement
  {
  public double thickness { get; set; }

  public double area { get; set; }

  public Level level { get; set; }

    // to implement source app parameters interface from claire
  }
}
