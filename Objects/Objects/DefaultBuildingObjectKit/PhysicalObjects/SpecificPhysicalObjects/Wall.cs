using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.enums;
using Objects.DefaultBuildingObjectKit.ProjectOrganization;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects.SpecificPhysicalObjects
{
  public class Wall : BuiltElement2D
  {
  public double height { get; set; }

  public wallType wallType{ get; set; }

  public Level Level { get; set; }
  }


}
