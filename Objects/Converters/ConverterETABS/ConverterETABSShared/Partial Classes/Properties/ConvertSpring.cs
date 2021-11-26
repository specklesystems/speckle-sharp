using System;
using System.Collections.Generic;
using Objects.Structural.Properties.Profiles;
using ETABSv1;
using System.Linq;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
    public ETABSSpringProperty SpringPropertyToSpeckle(string name)
    {
      double[] stiffness = null;
      int springOption = 0;
      string Cys = null;
      string SoilProfile = null;
      string Footing = null;
      double period = 0;
      int color = 0;
      string notes = null;
      string GUID = null;
      Model.PropPointSpring.GetPointSpringProp(name, ref springOption, ref stiffness, ref Cys, ref SoilProfile, ref Footing, ref period, ref color, ref notes, ref GUID);
      switch (springOption)
      {
        case 1:
          ETABSSpringProperty speckleSpringProperty = new ETABSSpringProperty(name, Cys, stiffness[0], stiffness[1], stiffness[2], stiffness[3], stiffness[4], stiffness[5]);
          return speckleSpringProperty;
        case 2:
          speckleSpringProperty = new ETABSSpringProperty(name, SoilProfile, Footing, period);
          return speckleSpringProperty;
        default:
          speckleSpringProperty = new ETABSSpringProperty();
          return speckleSpringProperty;
      }
    }
    public ETABSLinearSpring LinearSpringToSpeckle(string name)
    {
      ETABSLinearSpring speckleLinearSpring = new ETABSLinearSpring();
      return speckleLinearSpring;

    }
    public ETABSAreaSpring AreaSpringToSpeckle(string name)
    {
      ETABSAreaSpring speckleLinearSpring = new ETABSAreaSpring();
      return speckleLinearSpring;

    }
  }
}
