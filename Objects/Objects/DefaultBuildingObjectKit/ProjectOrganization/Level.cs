using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.DefaultBuildingObjectKit.ProjectOrganization
{
  public class Level:Base
  {
  public string name { get; set; }
    public double referenceElevation { get; set; }
    public double Elevation { get; set; }
  }
}
