using Speckle.Core.Kits;

namespace Objects.Structural.Properties;

public class PropertySpring : Property
{
  public PropertySpring() { }

  [SchemaInfo("PropertySpring", "Creates a Speckle structural spring property", "Structural", "Properties")]
  public PropertySpring(string name)
  {
    this.name = name;
  }

  [SchemaInfo(
    "PropertySpring (linear/elastic)",
    "Creates a Speckle structural spring property (linear/elastic spring)",
    "Structural",
    "Properties"
  )]
  public PropertySpring(
    string name,
    double stiffnessX = 0,
    double stiffnessY = 0,
    double stiffnessZ = 0,
    double stiffnessXX = 0,
    double stiffnessYY = 0,
    double stiffnessZZ = 0,
    double dampingRatio = 0
  )
  {
    this.name = name;
    springType = PropertyTypeSpring.General;
    this.stiffnessX = stiffnessX;
    this.stiffnessY = stiffnessY;
    this.stiffnessZ = stiffnessZ;
    this.stiffnessXX = stiffnessXX;
    this.stiffnessYY = stiffnessYY;
    this.stiffnessZZ = stiffnessZZ;
    this.dampingRatio = dampingRatio;
  }

  [SchemaInfo(
    "PropertySpring (non-linear)",
    "Creates a Speckle structural spring property (non-linear spring)",
    "Structural",
    "Properties"
  )]
  public PropertySpring(
    string name,
    double springCurveX = 0,
    double stiffnessX = 0,
    double springCurveY = 0,
    double stiffnessY = 0,
    double springCurveZ = 0,
    double stiffnessZ = 0,
    double springCurveXX = 0,
    double stiffnessXX = 0,
    double springCurveYY = 0,
    double stiffnessYY = 0,
    double springCurveZZ = 0,
    double stiffnessZZ = 0,
    double dampingRatio = 0
  )
  {
    this.name = name;
    springType = PropertyTypeSpring.General;
    this.springCurveX = springCurveX;
    this.springCurveY = springCurveY;
    this.springCurveZ = springCurveZ;
    this.springCurveXX = springCurveXX;
    this.springCurveYY = springCurveYY;
    this.springCurveZZ = springCurveZZ;
    this.stiffnessX = springCurveX == 0 ? stiffnessX : 0;
    this.stiffnessY = springCurveY == 0 ? stiffnessY : 0;
    this.stiffnessZ = springCurveZ == 0 ? stiffnessZ : 0;
    this.stiffnessXX = springCurveXX == 0 ? stiffnessXX : 0;
    this.stiffnessYY = springCurveYY == 0 ? stiffnessYY : 0;
    this.stiffnessZZ = springCurveZZ == 0 ? stiffnessZZ : 0;
    this.dampingRatio = dampingRatio;
  }

  public PropertyTypeSpring springType { get; set; }
  public double springCurveX { get; set; } //if 0 spring is elastic, otherwise refers to a material curve by number
  public double stiffnessX { get; set; }
  public double springCurveY { get; set; } //if 0 spring is elastic, otherwise refers to a material curve by number
  public double stiffnessY { get; set; }
  public double springCurveZ { get; set; } //if 0 spring is elastic, otherwise refers to a material curve by number
  public double stiffnessZ { get; set; }
  public double springCurveXX { get; set; } //if 0 spring is elastic, otherwise refers to a material curve by number
  public double stiffnessXX { get; set; }
  public double springCurveYY { get; set; } //if 0 spring is elastic, otherwise refers to a material curve by number
  public double stiffnessYY { get; set; }
  public double springCurveZZ { get; set; } //if 0 spring is elastic, otherwise refers to a material curve by number
  public double stiffnessZZ { get; set; }
  public double dampingRatio { get; set; }
  public double dampingX { get; set; } //is this needed? springType can't be set to DAMPER
  public double dampingY { get; set; } //is this needed? springType can't be set to DAMPER
  public double dampingZ { get; set; } //is this needed? springType can't be set to DAMPER
  public double dampingXX { get; set; } //is this needed? springType can't be set to DAMPER
  public double dampingYY { get; set; } //is this needed? springType can't be set to DAMPER
  public double dampingZZ { get; set; } //is this needed? springType can't be set to DAMPER
  public double matrix { get; set; } //refers to spring matrix record.
  public double positiveLockup { get; set; }
  public double negativeLockup { get; set; }
  public double frictionCoefficient { get; set; }
}
