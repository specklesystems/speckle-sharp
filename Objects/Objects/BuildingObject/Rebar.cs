using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.enums;
using Objects.Definitions;

namespace Objects.BuildingObject
{
  public class Rebar :CurveBasedElement
  {
  public double volume { get; set; }

  public rebarShape barShape { get; set; }
    // to implement source app parameters interface from claire
  }
}
