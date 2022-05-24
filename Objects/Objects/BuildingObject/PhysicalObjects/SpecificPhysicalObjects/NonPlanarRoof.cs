using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.BuildingObject.PhysicalObjects;

namespace Objects.BuildingObject.PhysicalObjects
{
  public class NonPlanarRoof : CurveBasedElement
  {

    // to implement source app parameters interface from claire
    public double volume { get; set; }
    public double surfaceArea { get; set; }

  }
}
