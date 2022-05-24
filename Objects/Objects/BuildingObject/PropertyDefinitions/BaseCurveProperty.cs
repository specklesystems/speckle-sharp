using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.enums;
using Speckle.Core.Models;

namespace Objects.BuildingObject.Calculations
{
  public class BaseCurveProperty : Base
  {
    public string name { get; set; }
    //public double thickness { get; set; }

    public CurveElementType type { get; set; }
  }
}