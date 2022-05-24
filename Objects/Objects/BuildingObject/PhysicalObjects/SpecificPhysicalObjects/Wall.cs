using System;
using System.Collections.Generic;
using System.Text;
using Objects.ProjectOrganization;
using Objects.BuildingObject.enums;

namespace Objects.BuildingObject.PhysicalObjects
{
  public class Wall : CurveBasedElement
  {
    public double height { get; set; }

    public Level topLevel { get; set; }

    public Level baseLevel { get; set; }

    public double topOffSet { get; set; }
    public double bottomOffSet { get; set; }
    public double thickness { get; set; }
    public double area { get; set; }

    // to implement source app parameters interface from claire

    public Wall()
    {
    }
  }
}