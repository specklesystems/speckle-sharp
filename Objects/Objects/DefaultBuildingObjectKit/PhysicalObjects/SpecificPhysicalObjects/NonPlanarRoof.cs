using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.PhysicalObjects;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class NonPlanarRoof : GenericFormElement
  {

    // to implement source app parameters interface from claire
    public double volume { get; set; }
    public double surfaceArea { get; set; }

  }
}
