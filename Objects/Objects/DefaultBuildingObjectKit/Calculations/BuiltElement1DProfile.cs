using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.Calculations
{
  public class BuiltElement1DProfile :Base
  {
   public ProfileType profileType { get; set; }
    public BuiltElement1DProfile() { }
  }
  public class Rectangular : BuiltElement1DProfile
  {
    public double depth { get; set; }
    public double width { get; set; }
    public Rectangular() { }

  }

  public class Circular : BuiltElement1DProfile
  {
    public double radius { get; set; }

    public Circular() { }


  }
  public class SemiCircular : BuiltElement1DProfile
  {
    public double radius { get; set; }

    public SemiCircular() { }

    //this needs some though in terms of orientation ~ maybe an orientation per element per plane ? 
  }
}
