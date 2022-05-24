using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.PhysicalObjects;
using Objects.DefaultBuildingObjectKit.ProjectOrganization;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class Duct : CurveBasedElement
  {
  public double diameter { get; set; }

  public Level level { get; set; }
    // to implement source app parameters interface from claire

  }
}
