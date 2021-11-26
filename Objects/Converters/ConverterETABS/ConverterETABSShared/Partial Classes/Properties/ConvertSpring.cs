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
  public ETABSSpringProperty SpringPropertyToSpeckle(string name){
      ETABSSpringProperty speckleSpringProperty = new ETABSSpringProperty();
      return speckleSpringProperty;
      
  }
  public ETABSLinearSpring LinearSpringToSpeckle(string name){
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
