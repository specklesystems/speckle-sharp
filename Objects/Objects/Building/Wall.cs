using System;
using System.Collections.Generic;
using System.Text;
using Objects.Organization;
using Objects.Building.enums;
using Objects.Definitions;

namespace Objects.Building
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