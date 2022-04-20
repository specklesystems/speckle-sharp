using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.Calculations
{
  public class BuiltElement3DProperty : Base
  {
    public string name { get; set; }
    public Element3DType Element3DType { get; set; }
    public Base parameters { get; set; }
  }
}
