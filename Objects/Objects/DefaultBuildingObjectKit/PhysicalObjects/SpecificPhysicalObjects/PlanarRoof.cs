using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Objects.DefaultBuildingObjectKit.ProjectOrganization;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class PlanarRoof : GenericFormElement
  {
  public double thickness { get; set; }

  public double area { get; set; }

  public Level level { get; set; }

    // to implement source app parameters interface from claire
  }
}
