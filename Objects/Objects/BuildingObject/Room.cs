using System;
using System.Collections.Generic;
using System.Text;
using Objects.Definitions;
using Objects.Organization;

namespace Objects.BuildingObject
{
  public class Room : PointBasedElement
  {
    public Level level { get; set; }
    public double area { get; set; }

    public double volume { get; set; }
    // to implement source app parameters interface from claire
  }
}