using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.DefaultBuildingObjectKit.ProjectOrganization
{
  public class Model
  {
    public ModelInfo ModelInfo {get;set;}
    public ModelSettings ModelSettings { get; set; }
  public List<Base> Elements { get; set; } 

  public List<Base> Properties { get; set; }
  public List<Base> Materials { get; set; }

  public List<Base> NonPhysicalObjects { get; set; }
  }
}
