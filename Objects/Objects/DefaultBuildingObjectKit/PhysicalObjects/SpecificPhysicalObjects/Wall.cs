using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.enums;
using Objects.DefaultBuildingObjectKit.ProjectOrganization;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class Wall : CurveBasedElement
  {
    public double height { get; set; }

    public Level TopLevel { get; set; }

    public Level BaseLevel { get; set; }

    public double topOffSet{ get; set; }
    public double bottomOffSet { get; set; }
    public double thickness { get; set; }
    public double area { get; set; }

    // to implement source app parameters interface from claire

  }


}
