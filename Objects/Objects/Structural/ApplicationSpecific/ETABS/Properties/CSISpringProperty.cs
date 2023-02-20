using Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.Materials;
using System;

namespace Objects.Structural.CSI.Properties
{
  public class CSISpringProperty: PropertySpring
  {
    public SpringOption springOption { get; set; }
    public string CYs { get; set; }
    public string SoilProfile { get; set; }
    public string footing { get; set; }
    public double period { get; set; }

    public CSISpringProperty()
    {
    }

    [SchemaInfo("PointSpring from Link", "Create an CSI PointSpring from Link", "CSI", "Properties")]
    public CSISpringProperty( string name, string cYs, double StiffnessX, double StiffnessY, double StiffnessZ, double StiffnessXX, double StiffnezzYY, double StiffnessZZ)
    {
      this.name = name;
      this.springOption = SpringOption.Link;
      this.stiffnessX = StiffnessX;
      this.stiffnessY = StiffnessY;
      this.stiffnessZ = StiffnessZ;
      this.stiffnessXX = StiffnessXX;
      this.stiffnessYY = StiffnezzYY;
      this.stiffnessZZ = StiffnessZZ;
      CYs = cYs;
    }

    [SchemaInfo("PointSpring from Soil Profile", "Create an CSI PointSpring from Soil Profile", "CSI", "Properties")]
    public CSISpringProperty( string name, string soilProfile, string footing, double period)
    {
      this.name = name;
      this.springOption = SpringOption.SoilProfileFooting;
      SoilProfile = soilProfile;
      this.footing = footing;
      this.period = period;
    }
  }
  public class CSILinearSpring : PropertySpring
  {
    public NonLinearOptions LinearOption1{ get; set; }
    public NonLinearOptions LinearOption2 { get; set; }

    public CSILinearSpring()
    {
    }

    [SchemaInfo("LinearSpring", "Create an CSI LinearSpring", "CSI", "Properties")]
    public CSILinearSpring(string name,double StiffnessX,double StiffnessY, double StiffnessZ, double StiffnessXX,NonLinearOptions linearOption1, NonLinearOptions linearOption2, string applicationID = null)
    {
      this.name = name;
      this.stiffnessX = StiffnessX;
      stiffnessY = StiffnessY;
      stiffnessZ = StiffnessZ;
      stiffnessXX = StiffnessXX;
      LinearOption1 = linearOption1;
      LinearOption2 = linearOption2;
      applicationId = applicationID;
    }
  }
  public class CSIAreaSpring : PropertySpring 
  {
    public NonLinearOptions LinearOption3 { get; set; }

    public CSIAreaSpring()
    {
    }

    [SchemaInfo("LinearSpring", "Create an CSI AreaSpring", "CSI", "Properties")]
    public CSIAreaSpring(string name, double StiffnessX, double StiffnessY, double StiffnessZ,  NonLinearOptions linearOption3, string applicationID = null)
    {
      this.name = name;
      this.stiffnessX = StiffnessX;
      stiffnessY = StiffnessY;
      stiffnessZ = StiffnessZ;
      LinearOption3 = linearOption3;
      applicationId = applicationID;
    }
  }
}
