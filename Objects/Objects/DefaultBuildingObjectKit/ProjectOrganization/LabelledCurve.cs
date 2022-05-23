using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.ProjectOrganization
{
  public class LabelledCurve : Base
  {
  //Replaces FeatureLine + GridLine
  public string name { get; set; }

  public ICurve baseCurve { get; set; }
    public double color { get; set; } // we should do something unique in terms of display of these guys in the viewers by default 
    public LabelType labelType { get; set; }
   }
}
