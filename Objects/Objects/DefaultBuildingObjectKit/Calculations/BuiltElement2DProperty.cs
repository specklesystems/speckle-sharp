using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.Calculations
{
  public class BuiltElement2DProperty : Base
  {
  public string name { get; set; }
  public double thickness { get; set; }

  public Element2DType element2DType { get; set; }
  public Base parameters { get; set; }
  }
}
