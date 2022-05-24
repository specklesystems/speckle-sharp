using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class Rebar :CurveBasedElement
  {
  public double volume { get; set; }

  public rebarShape barShape { get; set; }
    // to implement source app parameters interface from claire
  }
}
