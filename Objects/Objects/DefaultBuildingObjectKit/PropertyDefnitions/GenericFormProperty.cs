using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.Calculations
{
  public class GenericFormProperty : Base
  {
    public string name { get; set; }
    public GenericFormType Type { get; set; }
  }
}
