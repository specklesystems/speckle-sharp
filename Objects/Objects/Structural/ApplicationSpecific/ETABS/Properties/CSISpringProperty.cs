using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.CSI.Properties;

public class CSISpringProperty : PropertySpring
{
  public CSISpringProperty() { }

  [SchemaInfo("PointSpring from Link", "Create an CSI PointSpring from Link", "CSI", "Properties")]
  public CSISpringProperty(
    string name,
    string cYs,
    double StiffnessX,
    double StiffnessY,
    double StiffnessZ,
    double StiffnessXX,
    double StiffnezzYY,
    double StiffnessZZ
  )
  {
    this.name = name;
    springOption = SpringOption.Link;
    stiffnessX = StiffnessX;
    stiffnessY = StiffnessY;
    stiffnessZ = StiffnessZ;
    stiffnessXX = StiffnessXX;
    stiffnessYY = StiffnezzYY;
    stiffnessZZ = StiffnessZZ;
    CYs = cYs;
  }

  [SchemaInfo("PointSpring from Soil Profile", "Create an CSI PointSpring from Soil Profile", "CSI", "Properties")]
  public CSISpringProperty(string name, string soilProfile, string footing, double period)
  {
    this.name = name;
    springOption = SpringOption.SoilProfileFooting;
    SoilProfile = soilProfile;
    this.footing = footing;
    this.period = period;
  }

  public SpringOption springOption { get; set; }
  public string CYs { get; set; }
  public string SoilProfile { get; set; }
  public string footing { get; set; }
  public double period { get; set; }
}

public class CSILinearSpring : PropertySpring
{
  public CSILinearSpring() { }

  [SchemaInfo("LinearSpring", "Create an CSI LinearSpring", "CSI", "Properties")]
  public CSILinearSpring(
    string name,
    double StiffnessX,
    double StiffnessY,
    double StiffnessZ,
    double StiffnessXX,
    NonLinearOptions linearOption1,
    NonLinearOptions linearOption2,
    string applicationID = null
  )
  {
    this.name = name;
    stiffnessX = StiffnessX;
    stiffnessY = StiffnessY;
    stiffnessZ = StiffnessZ;
    stiffnessXX = StiffnessXX;
    LinearOption1 = linearOption1;
    LinearOption2 = linearOption2;
    applicationId = applicationID;
  }

  public NonLinearOptions LinearOption1 { get; set; }
  public NonLinearOptions LinearOption2 { get; set; }
}

public class CSIAreaSpring : PropertySpring
{
  public CSIAreaSpring() { }

  [SchemaInfo("LinearSpring", "Create an CSI AreaSpring", "CSI", "Properties")]
  public CSIAreaSpring(
    string name,
    double StiffnessX,
    double StiffnessY,
    double StiffnessZ,
    NonLinearOptions linearOption3,
    string applicationID = null
  )
  {
    this.name = name;
    stiffnessX = StiffnessX;
    stiffnessY = StiffnessY;
    stiffnessZ = StiffnessZ;
    LinearOption3 = linearOption3;
    applicationId = applicationID;
  }

  public NonLinearOptions LinearOption3 { get; set; }
}
