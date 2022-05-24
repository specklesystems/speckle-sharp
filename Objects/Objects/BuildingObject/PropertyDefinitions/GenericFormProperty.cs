using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.enums;
using Speckle.Core.Models;

namespace Objects.BuildingObject.Calculations
{
  public class GenericFormProperty : Base
  {
    public string name { get; set; }
    public GenericFormType Type { get; set; }
  }
}
