using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class Framing : CurveBasedElement
  {

    public ProfileType profileType { get; set; }

    public FramingType framingOrientation { get; set; }

    public double framingSurfaceArea {get;set;}
    public double end1OffSet { get; set; }
    public double end2OffSet { get; set; }

    // to implement source app parameters interface from claire

  }
}
