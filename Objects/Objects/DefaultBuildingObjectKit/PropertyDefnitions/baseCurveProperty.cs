using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.Calculations
{
  public class baseCurveProperty : Base
  {
  public string name { get; set; }
  //public double thickness { get; set; }

  public CurveElementType type { get; set; }
  }
}
