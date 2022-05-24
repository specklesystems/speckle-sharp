using System;
using System.Collections.Generic;
using System.Text;
using Objects.ProjectOrganization;

namespace Objects.BuildingObject.PhysicalObjects
{
  public class Room : PointBasedElement
  {
    public Level level { get; set; }
    public double area { get; set; }

    public double volume { get; set; }
    // to implement source app parameters interface from claire
  }
}