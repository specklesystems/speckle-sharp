using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.ProjectOrganization;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects.SpecificPhysicalObjects
{
  public class Room : PointElement
  {
  public Level level { get; set; }
  public double area { get; set; }
  public double volume { get; set; }
    // to implement source app parameters interface from claire
  }
}
