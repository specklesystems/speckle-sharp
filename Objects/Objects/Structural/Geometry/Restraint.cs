using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Geometry;

public class Restraint : Base
{
  public Restraint() { }

  [SchemaInfo("Restraint (by code)", "Creates a Speckle restraint object", "Structural", "Geometry")]
  public Restraint(
    [SchemaParamInfo(
      "A 6-character string to describe the restraint condition (F = Fixed, R = Released) for each degree of freedom - the first 3 characters represent translational degrees of freedom in the X, Y, and Z axes and the last 3 characters represent rotational degrees of freedom about the X, Y, and Z axes (ex. FFFRRR denotes a pinned condition, FFFFFF denotes a fixed condition)"
    )]
      string code
  )
  {
    this.code = code.ToUpper();
  }

  [SchemaInfo(
    "Restraint (by code and stiffness)",
    "Creates a Speckle restraint object (to describe support conditions with an explicit stiffness)",
    "Structural",
    "Geometry"
  )]
  public Restraint(
    [SchemaParamInfo(
      "A 6-character string to describe the restraint condition (F = Fixed, R = Released, K = Stiffness) for each degree of freedom - the first 3 characters represent translational degrees of freedom in the X, Y, and Z axes and the last 3 characters represent rotational degrees of freedom about the X, Y, and Z axes (ex. FFSRRR denotes fixed translation about the x and y axis, a spring stiffness for translation in the z axis and releases for all rotational degrees of freedom)"
    )]
      string code,
    [SchemaParamInfo("Applies only if the restraint code character for translation in x is 'K'")] double stiffnessX = 0,
    [SchemaParamInfo("Applies only if the restraint code character for translation in y is 'K'")] double stiffnessY = 0,
    [SchemaParamInfo("Applies only if the restraint code character for translation in z is 'K'")] double stiffnessZ = 0,
    [SchemaParamInfo("Applies only if the restraint code character for rotation about x is 'K'")]
      double stiffnessXX = 0,
    [SchemaParamInfo("Applies only if the restraint code character for rotation about y is 'K'")]
      double stiffnessYY = 0,
    [SchemaParamInfo("Applies only if the restraint code character for rotation about z is 'K'")] double stiffnessZZ = 0
  )
  {
    this.code = code.ToUpper();
    this.stiffnessX = code[0] == 'K' || code[0] == 'k' ? stiffnessX : 0;
    this.stiffnessY = code[1] == 'K' || code[1] == 'k' ? stiffnessY : 0;
    this.stiffnessZ = code[2] == 'K' || code[2] == 'k' ? stiffnessZ : 0;
    this.stiffnessXX = code[3] == 'K' || code[3] == 'k' ? stiffnessXX : 0;
    this.stiffnessYY = code[4] == 'K' || code[4] == 'k' ? stiffnessYY : 0;
    this.stiffnessZZ = code[5] == 'K' || code[5] == 'k' ? stiffnessZZ : 0;
  }

  [SchemaInfo(
    "Restraint (by enum)",
    "Creates a Speckle restraint object (for pinned condition or fixed condition)",
    "Structural",
    "Geometry"
  )]
  public Restraint(RestraintType restraintType)
  {
    if (restraintType == RestraintType.Free)
      code = "RRRRRR";
    if (restraintType == RestraintType.Pinned)
      code = "FFFRRR";
    if (restraintType == RestraintType.Fixed)
      code = "FFFFFF";
    if (restraintType == RestraintType.Roller)
      code = "RRFRRR";
  }

  public string code { get; set; } //a string to describe the restraint type for each degree of freedom - ex. FFFRRR (pin) / FFFFFF (fix)
  public double stiffnessX { get; set; }
  public double stiffnessY { get; set; }
  public double stiffnessZ { get; set; }
  public double stiffnessXX { get; set; }
  public double stiffnessYY { get; set; }
  public double stiffnessZZ { get; set; }
  public string units { get; set; }
}
