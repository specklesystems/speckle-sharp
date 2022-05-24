using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.enums;
using Objects.Definitions;
using Speckle.Core.Models;

namespace Objects.BuildingObject
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
